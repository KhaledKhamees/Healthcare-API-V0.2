using FluentAssertions;
using Healthcare_API.Models;
using Healthcare_API.Models.Enums;

namespace Healthcare_API_V0._2.Tests.Domain;

public sealed class RefreshTokenEntityTests
{
    [Fact]
    public void Ctor_WithEmptyUserId_Throws()
    {
        var act = () => new RefreshToken(Guid.Empty, "hash", DateTime.UtcNow.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_WithEmptyHash_Throws()
    {
        var act = () => new RefreshToken(Guid.NewGuid(), " ", DateTime.UtcNow.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsActive_TrueForFreshToken()
    {
        var t = new RefreshToken(Guid.NewGuid(), "h", DateTime.UtcNow.AddDays(1));
        t.IsActive.Should().BeTrue();
        t.IsExpired.Should().BeFalse();
        t.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_TrueForPastExpiry()
    {
        var t = new RefreshToken(Guid.NewGuid(), "h", DateTime.UtcNow.AddMinutes(-1));
        t.IsExpired.Should().BeTrue();
        t.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_IsIdempotent()
    {
        var t = new RefreshToken(Guid.NewGuid(), "h", DateTime.UtcNow.AddDays(1));
        t.Revoke();
        t.IsRevoked.Should().BeTrue();
        t.IsActive.Should().BeFalse();
        t.Revoke();
        t.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void MarkReplaced_SetsLineageAndRevokes()
    {
        var t = new RefreshToken(Guid.NewGuid(), "h", DateTime.UtcNow.AddDays(1));
        t.MarkReplaced("new-hash");
        t.ReplacedByTokenHash.Should().Be("new-hash");
        t.IsReplaced.Should().BeTrue();
        t.IsRevoked.Should().BeTrue();
        t.IsActive.Should().BeFalse();
    }

    [Fact]
    public void MarkReplaced_WithEmptyHash_Throws()
    {
        var t = new RefreshToken(Guid.NewGuid(), "h", DateTime.UtcNow.AddDays(1));
        var act = () => t.MarkReplaced(" ");
        act.Should().Throw<ArgumentException>();
    }
}

public sealed class DomainEntityTests
{
    [Fact]
    public void Doctor_StartsPending()
    {
        var d = new Doctor(Guid.NewGuid(), "A", "B", Guid.NewGuid());
        d.Status.Should().Be(DoctorStatus.Pending);
    }

    [Fact]
    public void Appointment_Cancel_SetsCancelled()
    {
        var a = new Appointment(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), "check");
        a.Status.Should().Be(AppointmentStatus.Scheduled);
        a.Cancel();
        a.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void DoctorAvailability_ActivateDeactivate_Toggles()
    {
        var av = new DoctorAvailability(Guid.NewGuid(), DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0));
        av.IsActive.Should().BeTrue();
        av.Deactivate();
        av.IsActive.Should().BeFalse();
        av.Activate();
        av.IsActive.Should().BeTrue();
    }
}
