using System.Linq.Expressions;

namespace FleetOps.Domain;

/// <summary>Specification pattern: composable, reusable query rules.</summary>
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T item) => ToExpression().Compile()(item);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
}

internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var l = left.ToExpression();
        var r = right.ToExpression();
        var p = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(Expression.Invoke(l, p), Expression.Invoke(r, p));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }
}

public sealed class ActiveVehicleSpecification : Specification<Vehicle>
{
    public override Expression<Func<Vehicle, bool>> ToExpression() => v => v.Status == VehicleStatus.Active;
}

public sealed class MileageAtLeastSpecification(int kilometres) : Specification<Vehicle>
{
    public override Expression<Func<Vehicle, bool>> ToExpression() => v => v.Mileage.Kilometres >= kilometres;
}
