using System.Reflection;
using FluentAssertions;
using Healthcare_API_V0._2_API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare_API_V0._2.Tests.API.Controllers;

public sealed class AuthorizationAttributeTests
{
    private static bool HasAuthorize(MemberInfo member) =>
        member.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Any();

    private static bool MethodHasAuthorize(Type controller, string method) =>
        controller.GetMethod(method)!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Any()
        || HasAuthorize(controller);

    [Fact]
    public void AuthController_IsAnonymous()
    {
        HasAuthorize(typeof(AuthController)).Should().BeFalse("login/register/refresh must stay public");
        typeof(AuthController).GetMethods()
            .Where(m => m.IsPublic && m.DeclaringType == typeof(AuthController))
            .Should().OnlyContain(m => !m.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any());
    }

    [Fact]
    public void AppointmentsController_RequiresAuthentication()
    {
        HasAuthorize(typeof(AppointmentsController)).Should().BeTrue();
    }

    [Fact]
    public void PatientsController_RequiresAuthentication()
    {
        HasAuthorize(typeof(PatientsController)).Should().BeTrue();
    }

    [Fact]
    public void Doctors_GetMe_Update_Delete_AssignClinic_RequireAuthentication()
    {
        MethodHasAuthorize(typeof(DoctorsController), nameof(DoctorsController.GetMe)).Should().BeTrue();
        MethodHasAuthorize(typeof(DoctorsController), nameof(DoctorsController.Update)).Should().BeTrue();
        MethodHasAuthorize(typeof(DoctorsController), nameof(DoctorsController.Delete)).Should().BeTrue();
        MethodHasAuthorize(typeof(DoctorsController), nameof(DoctorsController.AssignClinic)).Should().BeTrue();
    }

    [Fact]
    public void Doctors_GetById_And_GetBySpecialization_RemainPublic()
    {
        typeof(DoctorsController).GetMethod(nameof(DoctorsController.GetById))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().BeEmpty();
        typeof(DoctorsController).GetMethod(nameof(DoctorsController.GetBySpecialization))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().BeEmpty();
    }

    [Fact]
    public void Clinics_WriteOperations_RequireAuthentication()
    {
        MethodHasAuthorize(typeof(ClinicsController), nameof(ClinicsController.Create)).Should().BeTrue();
        MethodHasAuthorize(typeof(ClinicsController), nameof(ClinicsController.Update)).Should().BeTrue();
        MethodHasAuthorize(typeof(ClinicsController), nameof(ClinicsController.Delete)).Should().BeTrue();
    }

    [Fact]
    public void Specializations_WriteOperations_RequireAuthentication()
    {
        MethodHasAuthorize(typeof(SpecializationsController), nameof(SpecializationsController.Create)).Should().BeTrue();
        MethodHasAuthorize(typeof(SpecializationsController), nameof(SpecializationsController.Update)).Should().BeTrue();
        MethodHasAuthorize(typeof(SpecializationsController), nameof(SpecializationsController.Delete)).Should().BeTrue();
    }
}
