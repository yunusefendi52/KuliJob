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
        await Assert.That(() => jobData.GetValue<decimal>("decimal")).Throws<InvalidCastException>();
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
        await Assert.That(jobDataMap!.GetValue<decimal>("decimal")).IsEqualTo(decimal.MaxValue);
        await Assert.That(jobDataMap!.GetValue<double>("double")).IsEqualTo(double.MaxValue);
        await Assert.That(jobDataMap!.GetValue<DataSample>("data")).IsEqualTo(dataSample);
        await Assert.That(jobDataMap!.TryGetValue<string>("non_exist", out _)).IsEqualTo(false);
        await Assert.That(jobDataMap!.TryGetValue<DataSample>("data", out _)).IsEqualTo(true);
        await Assert.That(jobDataMap!.TryGetValue<DataSample>("data", out var _)).IsEqualTo(true);
        await Assert.That(jobDataMap!.GetValue<DataSample>("non_exists_data")).IsNull();
    }

    public record DataSample
    {
        public string? Value { get; set; }
    }
}
