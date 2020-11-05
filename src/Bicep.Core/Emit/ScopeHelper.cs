// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Deployments.Expression.Expressions;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem;
using Bicep.Core.TypeSystem.Az;

namespace Bicep.Core.Emit
{
    public static class ScopeHelper
    {
        public class ScopeData
        {
            public ResourceScopeType TargetScope { get; set; }

            public SyntaxBase? ManagementGroupNameProperty { get; set; }

            public SyntaxBase? SubscriptionIdProperty { get; set; }

            public SyntaxBase? ResourceGroupProperty { get; set; }
        }

        public static ScopeData GetScopeData(ResourceScopeType currentScope, TypeSymbol scopeType)
        {
            switch (currentScope)
            {
                case ResourceScopeType.TenantScope:
                    switch (scopeType)
                    {
                        case TenantScopeType tenantScopeType:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.TenantScope };
                        case ManagementGroupScopeType managementGroupScopeType when managementGroupScopeType.Arguments.Length == 1:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.ManagementGroupScope,
                                ManagementGroupNameProperty = managementGroupScopeType.Arguments[0].Expression };
                        case SubscriptionScopeType subscriptionScopeType when subscriptionScopeType.Arguments.Length == 1:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.SubscriptionScope, 
                                SubscriptionIdProperty = subscriptionScopeType.Arguments[0].Expression };
                    }
                    break;
                case ResourceScopeType.ManagementGroupScope:
                    switch (scopeType)
                    {
                        case TenantScopeType tenantScopeType:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.TenantScope };
                        case ManagementGroupScopeType managementGroupScopeType when managementGroupScopeType.Arguments.Length == 0:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.ManagementGroupScope };
                        case ManagementGroupScopeType managementGroupScopeType when managementGroupScopeType.Arguments.Length == 1:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.ManagementGroupScope, 
                                ManagementGroupNameProperty = managementGroupScopeType.Arguments[0].Expression };
                        case SubscriptionScopeType subscriptionScopeType when subscriptionScopeType.Arguments.Length == 1:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.SubscriptionScope, 
                                SubscriptionIdProperty = subscriptionScopeType.Arguments[0].Expression };
                    }
                    break;
                case ResourceScopeType.SubscriptionScope:
                    switch (scopeType)
                    {
                        case TenantScopeType tenantScopeType:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.TenantScope };
                        case SubscriptionScopeType subscriptionScopeType when subscriptionScopeType.Arguments.Length == 0:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.SubscriptionScope };
                        case ResourceGroupScopeType resourceGroupScopeType when resourceGroupScopeType.Arguments.Length == 1:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.ResourceGroupScope, 
                                ResourceGroupProperty = resourceGroupScopeType.Arguments[0].Expression };
                        case ResourceGroupScopeType resourceGroupScopeType when resourceGroupScopeType.Arguments.Length == 2:
                            return new ScopeData {
                                TargetScope = ResourceScopeType.ResourceGroupScope,
                                SubscriptionIdProperty = resourceGroupScopeType.Arguments[0].Expression,
                                ResourceGroupProperty = resourceGroupScopeType.Arguments[1].Expression };
                    }
                    break;
                case ResourceScopeType.ResourceGroupScope:
                    switch (scopeType)
                    {
                        case TenantScopeType tenantScopeType:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.TenantScope };
                        case ResourceGroupScopeType resourceGroupScopeType when resourceGroupScopeType.Arguments.Length == 0:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.ResourceGroupScope };
                        case ResourceGroupScopeType resourceGroupScopeType when resourceGroupScopeType.Arguments.Length == 1:
                            return new ScopeData { 
                                TargetScope = ResourceScopeType.ResourceGroupScope,
                                ResourceGroupProperty = resourceGroupScopeType.Arguments[0].Expression };
                        case ResourceGroupScopeType resourceGroupScopeType when resourceGroupScopeType.Arguments.Length == 2:
                            return new ScopeData {
                                TargetScope = ResourceScopeType.ResourceGroupScope,
                                SubscriptionIdProperty = resourceGroupScopeType.Arguments[0].Expression,
                                ResourceGroupProperty = resourceGroupScopeType.Arguments[1].Expression };
                    }
                    break;
            }

            throw new NotImplementedException($"Cannot generate scope for scope {currentScope}, type {scopeType}");
        }

        public static LanguageExpression FormatResourceId(ExpressionConverter expressionConverter, ScopeData scopeData, string fullyQualifiedType, IEnumerable<LanguageExpression> nameSegments)
        {
            var arguments = new List<LanguageExpression>();

            switch (scopeData.TargetScope)
            {
                case ResourceScopeType.TenantScope:
                    arguments.Add(new JTokenExpression(fullyQualifiedType));
                    arguments.AddRange(nameSegments);

                    return new FunctionExpression("tenantResourceId", arguments.ToArray(), new LanguageExpression[0]);
                case ResourceScopeType.SubscriptionScope:
                    if (scopeData.SubscriptionIdProperty != null)
                    {
                        arguments.Add(expressionConverter.ConvertExpression(scopeData.SubscriptionIdProperty));
                    }
                    arguments.Add(new JTokenExpression(fullyQualifiedType));
                    arguments.AddRange(nameSegments);

                    return new FunctionExpression("subscriptionResourceId", arguments.ToArray(), new LanguageExpression[0]);
                case ResourceScopeType.ResourceGroupScope:
                    LanguageExpression scope;
                    if (scopeData.SubscriptionIdProperty == null)
                    {
                        if (scopeData.ResourceGroupProperty == null)
                        {
                            scope = new FunctionExpression("resourceGroup", new LanguageExpression[0], new LanguageExpression[] { new JTokenExpression("id") });
                        }
                        else
                        {
                            var subscriptionId = new FunctionExpression("subscription", new LanguageExpression[0], new LanguageExpression[] { new JTokenExpression("subscriptionId") });
                            var resourceGroup = expressionConverter.ConvertExpression(scopeData.ResourceGroupProperty);
                            scope = ExpressionConverter.GenerateResourceGroupScope(subscriptionId, resourceGroup);
                        }
                    }
                    else
                    {
                        if (scopeData.ResourceGroupProperty == null)
                        {
                            throw new NotImplementedException($"Cannot format resourceId with non-null subscription and null resourceGroup");
                        }

                        var subscriptionId = expressionConverter.ConvertExpression(scopeData.SubscriptionIdProperty);
                        var resourceGroup = expressionConverter.ConvertExpression(scopeData.ResourceGroupProperty);
                        scope = ExpressionConverter.GenerateResourceGroupScope(subscriptionId, resourceGroup);
                    }

                    // We've got to DIY it, unfortunately. The resourceId() function behaves differently when used at different scopes, so is unsuitable here.
                    return ExpressionConverter.GenerateScopedResourceId(scope, fullyQualifiedType, nameSegments);
                case ResourceScopeType.ManagementGroupScope:
                    if (scopeData.ManagementGroupNameProperty != null)
                    {
                        var managementGroupName = expressionConverter.ConvertExpression(scopeData.ManagementGroupNameProperty);
                        var managementGroupScope = ExpressionConverter.GetManagementGroupScopeExpression(managementGroupName);

                        return ExpressionConverter.GenerateScopedResourceId(managementGroupScope, fullyQualifiedType, nameSegments);
                    }

                    // We need to do things slightly differently for Management Groups, because there is no IL to output for "Give me a fully-qualified resource id at the current scope",
                    // and we don't even have a mechanism for reliably getting the current scope (e.g. something like 'deployment().scope'). There are plans to add a managementGroupResourceId function,
                    // but until we have it, we should generate unqualified resource Ids. There should not be a risk of collision, because we do not allow mixing of resource scopes in a single bicep file.
                    return ExpressionConverter.GenerateUnqualifiedResourceId(fullyQualifiedType, nameSegments);
                default:
                    throw new NotImplementedException($"Cannot format resourceId for scope {scopeData.TargetScope}");
            }
        }

        public static void EmitModuleScopeProperties(ScopeData scopeData, ExpressionEmitter expressionEmitter)
        {
            switch (scopeData.TargetScope)
            {
                case ResourceScopeType.TenantScope:
                    expressionEmitter.EmitProperty("scope", new JTokenExpression("/"));
                    return;
                case ResourceScopeType.ManagementGroupScope:
                    if (scopeData.ManagementGroupNameProperty != null)
                    {
                        expressionEmitter.EmitProperty("scope", () => expressionEmitter.EmitManagementGroupScope(scopeData.ManagementGroupNameProperty));
                    }
                    return;
                case ResourceScopeType.SubscriptionScope:
                case ResourceScopeType.ResourceGroupScope:
                    if (scopeData.SubscriptionIdProperty != null)
                    {
                        expressionEmitter.EmitProperty("subscriptionId", scopeData.SubscriptionIdProperty);
                    }
                    if (scopeData.ResourceGroupProperty != null)
                    {
                        expressionEmitter.EmitProperty("resourceGroup", scopeData.ResourceGroupProperty);
                    }
                    return;
                default:
                    throw new NotImplementedException($"Cannot format resourceId for scope {scopeData.TargetScope}");
            }
        }
    }
}