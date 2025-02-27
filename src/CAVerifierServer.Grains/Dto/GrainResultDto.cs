namespace CAVerifierServer.Grains.Dto;

[GenerateSerializer]
public class GrainResultDto<T> : GrainResultDto
{
    [Id(0)]
    public T Data { get; set; }
}

[GenerateSerializer]
public class GrainResultDto
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public string Message { get; set; } = string.Empty;
}