namespace LabProject.Application.Mapper.CustomMapper;
using LabProject.Domain.Entities;
using LabProject.Application.DTOs;

public static class CustomMapper
{
    public static Contract ToEntity(ContractDTO dto)
    {
        return new Contract(
            dto.UserId,
            dto.MessageId,
            dto.Channels,
            dto.Attempt,
            dto.Source,
            dto.CreatedAt,
            dto.Metadata
        );
    }

    public static (ContractDTO?, IEnumerable<ContractDTO>?)? ToDTO(Contract contract, IEnumerable<Contract> contracts)
    {
        if (contract is not null || contracts is null)
        {
            var singleContract = new ContractDTO(
                contract!.UserId,
                contract.MessageId,
                contract.Channels,
                contract.Attempt,
                contract.Source,
                contract.CreatedAt,
                contract.Metadata
            );
            return (singleContract, null!)!;
        }

        if (contract is null || contracts is not null)
        {
            var contractsDTO = contracts.Select(c => new ContractDTO(
                c.UserId,
                c.MessageId,
                c.Channels,
                c.Attempt,
                c.Source,
                c.CreatedAt,
                c.Metadata
            ));
            return (null!, contractsDTO);
        }

        return null;
    }
}