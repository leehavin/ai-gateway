// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.Workflow.Services.InstanceDtos;
public class LogicFlowDto
{
    public LogicNode[] Nodes { get; set; } = [];

    public LogicEdge[] Edges { get; set; } = [];
}

public class LogicNode
{
    public string Id { get; set; }

    public string Type { get; set; }

    public Dictionary<string, object> Properties { get; set; }

    public LogicText Text { get; set; }

    public string GetWorkflowStepType()
    {
        return Type switch
        {
            "Start" => "AIAdmin.Workflow.Steps.StartSetp,AIAdmin.Workflow",
            "End" => "AIAdmin.Workflow.Steps.OverStep,AIAdmin.Workflow",
            "GeneralAuditing" => "AIAdmin.Workflow.Steps.GeneralAuditingStep,AIAdmin.Workflow",
            _ => "",
        };
    }
}

public class LogicText
{
    public string Value { get; set; }
}

public class LogicEdge
{
    public string Id { get; set; }

    public string Type { get; set; }

    public string SourceNodeId { get; set; }

    public string TargetNodeId { get; set; }

    public ConditionDto? Properties { get; set; }

    public LogicText Text { get; set; }
}

public class ConditionDto
{
    public string Field { get; set; }
    public string Value { get; set; }
    public string Operate { get; set; }
}