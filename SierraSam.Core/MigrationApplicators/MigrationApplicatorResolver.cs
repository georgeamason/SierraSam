namespace SierraSam.Core.MigrationApplicators;

public sealed class MigrationApplicatorResolver : IMigrationApplicatorResolver
{
    private readonly IEnumerable<IMigrationApplicator> _applicators;

    public MigrationApplicatorResolver(IEnumerable<IMigrationApplicator> applicators)
    {
        _applicators = applicators ?? throw new ArgumentNullException(nameof(applicators));
    }

    public IMigrationApplicator Resolve(Type type)
    {
        return _applicators.FirstOrDefault(a => a.GetType() == type)
               ?? throw new ArgumentException($"{type.Name} is not a registered migration applicator");
    }
}