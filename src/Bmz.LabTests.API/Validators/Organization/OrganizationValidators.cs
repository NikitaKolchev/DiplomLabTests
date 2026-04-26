using Bmz.LabTests.API.Contracts.Organization;
using FluentValidation;

namespace Bmz.LabTests.API.Validators.Organization;

public sealed class CreateEngineerRequestValidator : AbstractValidator<CreateEngineerRequest>
{
    public CreateEngineerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.LaboratoryId).GreaterThan(0).When(x => x.LaboratoryId.HasValue);
    }
}

public sealed class CreateAssistantByAdminRequestValidator : AbstractValidator<CreateAssistantByAdminRequest>
{
    public CreateAssistantByAdminRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.LaboratoryId).GreaterThan(0);
    }
}

public sealed class CreateAssistantByEngineerRequestValidator : AbstractValidator<CreateAssistantByEngineerRequest>
{
    public CreateAssistantByEngineerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class CreateLaboratoryRequestValidator : AbstractValidator<CreateLaboratoryRequest>
{
    public CreateLaboratoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EngineerId).GreaterThan(0).When(x => x.EngineerId.HasValue);
    }
}

public sealed class AssignEngineerRequestValidator : AbstractValidator<AssignEngineerRequest>
{
    public AssignEngineerRequestValidator()
    {
        RuleFor(x => x.EngineerId).GreaterThan(0);
    }
}

public sealed class UpdateAssistantRequestValidator : AbstractValidator<UpdateAssistantRequest>
{
    public UpdateAssistantRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}

public sealed class UpdateLaboratoryRequestValidator : AbstractValidator<UpdateLaboratoryRequest>
{
    public UpdateLaboratoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EngineerId).GreaterThan(0).When(x => x.EngineerId.HasValue);
    }
}

public sealed class UpdateEngineerRequestValidator : AbstractValidator<UpdateEngineerRequest>
{
    public UpdateEngineerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.Password));
        RuleFor(x => x.LaboratoryId).GreaterThan(0).When(x => x.LaboratoryId.HasValue);
    }
}

public sealed class UpdateAssistantByAdminRequestValidator : AbstractValidator<UpdateAssistantByAdminRequest>
{
    public UpdateAssistantByAdminRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.Password));
        RuleFor(x => x.LaboratoryId).GreaterThan(0);
    }
}
