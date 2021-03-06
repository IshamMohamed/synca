using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public class AsyncMethodGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a factory that can create our custom syntax receiver
        context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // the generator infrastructure will create a receiver and populate it
        // we can retrieve the populated instance via the context
        MySyntaxReceiver syntaxReceiver = (MySyntaxReceiver)context.SyntaxReceiver;

        // get the recorded user class
        //ClassDeclarationSyntax userClass = syntaxReceiver.ClassToAugment;
        foreach (var userClass in syntaxReceiver.CandidateClasses)
        {
            if (userClass is null)
            {
                // if we didn't find the user class, there is nothing to do
                return;
            }

            var model = context.Compilation.GetSemanticModel(userClass.SyntaxTree);
            var classInfo = model.GetDeclaredSymbol(userClass);
            var namespaceName = GetFullyQualifiedNamespace(classInfo.ContainingNamespace);

            // Full class generation
            StringBuilder stringBuilder = new StringBuilder(
                $@"   
                using System;
                using System.Net;
                using System.Reflection;
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.Extensions.Caching.Memory;
                using Microsoft.Extensions.Caching.Distributed;
                
                // reference to synca.lib
                using synca.lib.Hosted.Service;
                using synca.lib.Background;
                using synca.lib.Services;

                namespace {namespaceName}
                {{
                    public partial class {userClass.Identifier} : ControllerBase
                    {{"
            );

            stringBuilder.Append(GenerateMethods(userClass.Members));
            // Closing bracket for class
            stringBuilder.Append("}");
            // Closing bracket for namespace
            stringBuilder.Append("}");
            
            SourceText sourceText = SourceText.From(stringBuilder.ToString(), Encoding.UTF8);

            context.AddSource($"{userClass.Identifier}.Generated.cs", sourceText);
        }
        
    }

    
    private string GenerateMethods(SyntaxList<MemberDeclarationSyntax> members)
    {
        StringBuilder stringBuilder = new StringBuilder();

        foreach (var member in members)
        {
            if(member is MethodDeclarationSyntax method && HasRouteAttribute(method))
            {
                stringBuilder.Append(GenerateAsyncAcceptorMethod(method));
                stringBuilder.Append("\r\n");
                stringBuilder.Append(GenerateResultsCheckMethod(method));
            }
        }
        
        return stringBuilder.ToString();
    }

    private bool HasRouteAttribute(MethodDeclarationSyntax method)
    {
        var hasRouteAttribute = false;
        foreach (var attributeList in method.AttributeLists)
        {
            foreach(var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString().Equals("Route"))
                {
                    hasRouteAttribute = true;
                }
            }
        }

        return hasRouteAttribute;                
    }

    private string GenerateAsyncAcceptorMethod(MethodDeclarationSyntax method)
    {
        StringBuilder stringBuilder = new StringBuilder();
        
        // Generate method attributes
        // If Route is defined, this will prefix it with "async/". Eg:
        // [Route("Async<existing_route_artuments>")]
        foreach (var attributeList in method.AttributeLists)
        {
            foreach(var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString().Equals("Route"))
                {
                    stringBuilder.Append($@"[Route(""Async/{attribute.ArgumentList.Arguments[0].ToString().Trim('"')}"")]");
                }
                else
                {
                    stringBuilder.Append($"[{attribute}]");
                    stringBuilder.Append("\r\n");
                }
            }
        }
        stringBuilder.Append("\r\n");
        stringBuilder.Append("#pragma warning disable 1998");
        stringBuilder.Append("\r\n");
        // Create the method signature with method name renamed to "Async{methodName}"
        stringBuilder.Append($"{method.Modifiers} {method.ReturnType} Async{method.Identifier} ");
        stringBuilder.Append($"({method.ParameterList.Parameters})");
        stringBuilder.Append("\r\n");

        // Generate method body
        stringBuilder.Append($@"
            {{
                string taskId = Guid.NewGuid().ToString();
                string responseValue = $@""[host]/{{{{controller_route}}}}/GetResult{method.Identifier}/{{taskId}}"";

                _queue.QueueBackgroundWorkItem(token => {{return(taskId, {GenerateCallingMethod(method)});}});
                string cacheValue = $""{{((int)HttpStatusCode.Accepted).ToString()}}{{Convert.ToChar(31)}}{{responseValue}}"";
                _cache.SetString(taskId, cacheValue);
                return Accepted(responseValue);
            }}
        ");

        return stringBuilder.ToString();
    }
    

    private string GenerateResultsCheckMethod(MethodDeclarationSyntax method)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("#pragma warning disable 1998");
        stringBuilder.Append("\r\n");
        stringBuilder.Append("[HttpGet]");
        stringBuilder.Append("\r\n");
        stringBuilder.Append($@"[Route(""GetResult{method.Identifier}/{{id?}}"")]");
        stringBuilder.Append("\r\n");

        stringBuilder.Append($@"
        public IActionResult GetResult{method.Identifier}(string id)
        {{
            if(!string.IsNullOrEmpty(id))
            {{
                string cachedValue = string.Empty;
                
                cachedValue = _cache.GetString(id);
                if(!string.IsNullOrEmpty(cachedValue))
                {{
                    string statusCodeString = cachedValue.Split(Convert.ToChar(31))[0];
                    string serializedResonse = cachedValue.Split(Convert.ToChar(31))[1];
                    switch (int.Parse(statusCodeString))
                    {{
                        case ((int)HttpStatusCode.Accepted):
                            return Accepted(serializedResonse);
                        case ((int)HttpStatusCode.OK):
                            return Ok(serializedResonse);
                        case ((int)HttpStatusCode.BadRequest):
                            return BadRequest(serializedResonse);
                        case ((int)HttpStatusCode.Unauthorized):
                            return Unauthorized(serializedResonse);
                        case ((int)HttpStatusCode.Forbidden):
                            return Forbid(serializedResonse);
                        case ((int)HttpStatusCode.NotFound):
                            return NotFound(serializedResonse);
                        default:
                            return BadRequest(serializedResonse);
                    }}
                }}
                else
                {{
                    return BadRequest(""Unable to find request!"");
                }}

            }}
            else
            {{
                return BadRequest(""Id can't be empty or null"");
            }}
        }}"
        );
        return stringBuilder.ToString();
    }

    
    private string GetFullyQualifiedNamespace(INamespaceSymbol namespaceSymbol)
    {
        StringBuilder stringBuilder = new StringBuilder(namespaceSymbol.Name);

        if (!namespaceSymbol.ContainingNamespace.IsGlobalNamespace)
        { 
            stringBuilder.Insert(0, $"{GetFullyQualifiedNamespace(namespaceSymbol.ContainingNamespace)}.");
        }

        return stringBuilder.ToString();
    }

    private  string GenerateCallingMethod(MethodDeclarationSyntax method)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(method.Identifier);
            stringBuilder.Append("(");
            foreach(var parameter in  method.ParameterList.Parameters)
            {
                stringBuilder.Append($"{parameter.Identifier}");
                if (method.ParameterList.Parameters.Last() != parameter)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append(")");

            return stringBuilder.ToString();
        }
    
    class MySyntaxReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax ClassToAugment { get; private set; }
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();


        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Business logic to decide what we're interested in goes here
            if (syntaxNode is ClassDeclarationSyntax cds &&
                cds.Identifier.ValueText.EndsWith("Controller") &&
                cds.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                //ClassToAugment = cds;
                CandidateClasses.Add(cds);
            }
        }
    }
}