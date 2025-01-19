using KuliJob.Internals;

namespace KuliJob.Tests;

public class SerializerTest
{
    [Test]
    public Task CanSerialize_Empty_JobData()
    {
        var serializer = new Serializer();
        return Verify(serializer.Serialize(new JobDataMap()));
    }

    [Test]
    public Task CanDeserialize_Empty_JobData()
    {
        var serializer = new Serializer();
        return Verify(serializer.Serialize(serializer.Deserialize<JobDataMap>(serializer.Serialize(new JobDataMap()))));
    }

    static JobDataMap GetJobDataMap()
    {
        return new()
        {
            { "string", "stringvalue" },
            { "StringValue", "StringValue" },
            { "int", int.MaxValue },
            { "long", long.MaxValue },
            { "DoubleMax", double.MaxValue },
            { "DoubleMin", double.MinValue },
            { "Decimal", decimal.MaxValue},
        };
    }

    [Test]
    public async Task CanSerialize_JobData()
    {
        var serializer = new Serializer();
        await Verify(serializer.Serialize(GetJobDataMap()));
    }

    [Test]
    public async Task CanDeserialize_JobData()
    {
        var serializer = new Serializer();
        await Verify(serializer.Serialize(serializer.Deserialize<JobDataMap>(serializer.Serialize(GetJobDataMap()))));
    }

    [Test]
    public async Task ShouldThrowsJobDataMap_WithoutSerialize()
    {
        var jobData = new JobDataMap()
        {
            {"decimal", decimal.MaxValue},
        };
        await Assert.That(() => jobData.GetDecimal("decimal")).Throws<InvalidCastException>();
    }

    [Test]
    public async Task CanDeserialize_AndGetValue_JobDataMap()
    {
        var dataSample = new DataSample
        {
            Value = "value",
        };
        var serializer = new Serializer();
        var jobDataMap = serializer.Deserialize<JobDataMap>(serializer.Serialize(new JobDataMap
        {
            {"decimal", decimal.MaxValue},
            {"double", double.MaxValue},
            {"data", dataSample},
        }));;
        await Assert.That(jobDataMap!.GetDecimal("decimal")).IsEqualTo(decimal.MaxValue);
        await Assert.That(jobDataMap!.GetDouble("double")).IsEqualTo(double.MaxValue);
        await Assert.That(jobDataMap!.Get<DataSample>("data")).IsEqualTo(dataSample);
    }

    public record DataSample
    {
        public string? Value { get; set; }
    }
}
