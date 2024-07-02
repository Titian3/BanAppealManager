using BanAppealManager.Main.Models;

namespace BanAppealManager.Main.Scrapers.SS14Admin;

public class RoleBanGrouper
{
    private static readonly Dictionary<string, string> RoleToDepartmentMap = new()
    {
        { "Job:Janitor", "Service" },
        { "Job:Botanist", "Service" },
        { "Job:Bartender", "Service" },
        { "Job:Chef", "Service" },
        { "Job:Clown", "Service" },
        { "Job:Mime", "Service" },
        { "Job:ServiceWorker", "Service" },
        { "Job:Chaplain", "Service" },
        { "Job:Musician", "Service" },
        { "Job:Borg", "Service" },
        { "Job:Lawyer", "Service" },
        { "Job:Librarian", "Service" },
        { "Job:Reporter", "Service" },
        { "Job:Boxer", "Service" },
        { "Job:Zookeeper", "Service" },
        { "Job:Passenger", "Service" },
        { "Job:Scientist", "Science" },
        { "Job:ResearchAssistant", "Science" },
        { "Job:SeniorResearcher", "Science" },
        { "Job:StationEngineer", "Engineering" },
        { "Job:AtmosphericTechnician", "Engineering" },
        { "Job:TechnicalAssistant", "Engineering" },
        { "Job:SeniorEngineer", "Engineering" },
        { "Job:SalvageSpecialist", "Cargo" },
        { "Job:CargoTechnician", "Cargo" },
        { "Job:MedicalDoctor", "Medical" },
        { "Job:Chemist", "Medical" },
        { "Job:MedicalIntern", "Medical" },
        { "Job:Paramedic", "Medical" },
        { "Job:Psychologist", "Medical" },
        { "Job:SeniorPhysician", "Medical" },
        { "Job:Captain", "Command" },
        { "Job:ChiefMedicalOfficer", "Command" },
        { "Job:HeadOfPersonnel", "Command" },
        { "Job:Quartermaster", "Command" },
        { "Job:HeadOfSecurity", "Command" },
        { "Job:ResearchDirector", "Command" },
        { "Job:ChiefEngineer", "Command" },
        { "Job:CentralCommandOfficial", "Command" },
        { "Job:SecurityOfficer", "Security" },
        { "Job:SecurityCadet", "Security" },
        { "Job:Warden", "Security" },
        { "Job:Detective", "Security" },
        { "Job:SeniorOfficer", "Security" },
        { "Job:Brigmedic", "Security" }
    };

    public static List<GroupedRoleBan> GroupRoleBans(List<RoleBanDetails> roleBans)
    {
        var groupedRoleBans = roleBans
            .GroupBy(rb => new { rb.Reason, rb.BanTime, rb.ExpireTime, rb.UnbanTime, rb.UnbannedBy })
            .Select(g => new GroupedRoleBan
            {
                Reason = g.Key.Reason,
                BanTime = g.Key.BanTime,
                ExpireTime = g.Key.ExpireTime,
                UnbanTime = g.Key.UnbanTime,
                UnbannedBy = g.Key.UnbannedBy,
                Roles = g.Select(rb => rb.Role).ToList(),
                Departments = g.Select(rb => RoleToDepartmentMap.ContainsKey(rb.Role) ? RoleToDepartmentMap[rb.Role] : "Unknown")
                                .Distinct()
                                .ToList()
            })
            .ToList();

        return groupedRoleBans;
    }
}
