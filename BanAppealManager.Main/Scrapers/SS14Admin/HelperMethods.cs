using BanAppealManager.Main.Models;

namespace BanAppealManager.Main.Scrapers.SS14Admin
{
    public static class HelperMethods
    {
        public static int CalculateBanTimeServed(Userdetails userDetails)
        {
            var oldestActiveBan = userDetails.Banlist
                .Where(ban => ban.IsActive)
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            if (oldestActiveBan != null)
            {
                return (DateTime.Now - oldestActiveBan.BanTime).Days;
            }

            return 0;
        }

        public static int CalculateRoleBanTimeServed(Userdetails userDetails)
        {
            var oldestRoleBan = userDetails.RoleBanlist
                .Where(ban => ban.IsActive || ban.IsExpired)
                .OrderBy(b => b.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            if (oldestRoleBan != null)
            {
                return (DateTime.Now - oldestRoleBan.BanTime).Days;
            }

            return 0;
        }

        public static string GetLatestBanReason(Userdetails userDetails)
        {
            var oldestBan = userDetails.Banlist
                .Where(ban => ban.IsActive)
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            return oldestBan != null ? oldestBan.Reason : "No ban reason found.";
        }

        public static string GetLatestRoleBanReason(Userdetails userDetails)
        {
            var oldestRoleBan = userDetails.RoleBanlist
                .Where(ban => ban.IsActive || ban.IsExpired)
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            return oldestRoleBan?.Reason ?? "No active or expired role bans found.";
        }

        public static string GetLatestRoundNumber(Userdetails userDetails)
        {
            var oldestBan = userDetails.Banlist
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            return oldestBan != null ? oldestBan.RoundNumber : "No round number found.";
        }

        public static string GetLatestRoleRoundNumber(Userdetails userDetails)
        {
            var oldestRoleBan = userDetails.RoleBanlist
                .Where(ban => ban.IsActive || ban.IsExpired)
                .OrderBy(ban => ban.BanTime) // Sort by BanTime in ascending order
                .FirstOrDefault();

            return oldestRoleBan?.RoundNumber ?? "No active or expired role round number found.";
        }

        public static Dictionary<string, List<string>> ClassifyRoleBans(List<RoleBanDetails> roleBanDetails)
        {
            var departmentRoleMapping = new Dictionary<string, List<string>>
            {
                {
                    "Service",
                    new List<string>
                    {
                        "Janitor", "Botanist", "Bartender", "Chef", "Clown", "Mime", "ServiceWorker", "Chaplain",
                        "Musician", "Borg", "Lawyer", "Librarian", "Reporter", "Boxer", "Zookeeper", "Passenger"
                    }
                },
                { "Science", new List<string> { "Scientist", "ResearchAssistant", "SeniorResearcher" } },
                {
                    "Engineering",
                    new List<string>
                        { "StationEngineer", "AtmosphericTechnician", "TechnicalAssistant", "SeniorEngineer" }
                },
                { "Cargo", new List<string> { "SalvageSpecialist", "CargoTechnician" } },
                {
                    "Medical",
                    new List<string>
                        { "MedicalDoctor", "Chemist", "MedicalIntern", "Paramedic", "Psychologist", "SeniorPhysician" }
                },
                {
                    "Command",
                    new List<string>
                    {
                        "Captain", "ChiefMedicalOfficer", "HeadOfPersonnel", "Quartermaster", "HeadOfSecurity",
                        "ResearchDirector", "ChiefEngineer", "CentralCommandOfficial"
                    }
                },
                {
                    "Security",
                    new List<string>
                        { "SecurityOfficer", "SecurityCadet", "Warden", "Detective", "SeniorOfficer", "Brigmedic" }
                }
            };

            var groupedRoleBans = new Dictionary<string, List<string>>();

            foreach (var roleBan in roleBanDetails)
            {
                foreach (var department in departmentRoleMapping)
                {
                    if (department.Value.Contains(roleBan.Role))
                    {
                        if (!groupedRoleBans.ContainsKey(department.Key))
                        {
                            groupedRoleBans[department.Key] = new List<string>();
                        }

                        groupedRoleBans[department.Key].Add(roleBan.Role);
                    }
                }
            }

            return groupedRoleBans;
        }
    }
}
