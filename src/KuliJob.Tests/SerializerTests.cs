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
}
