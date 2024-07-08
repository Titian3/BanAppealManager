using BanAppealManager.Main.Models;

namespace BanAppealManager.Main.Scrapers.Forums.Topic.BanDiscussions
{
    public class VoteRulesEngine
    {
        private const int DefaultReduction = 1;

        public VoteOutcome DetermineOutcome(BanDiscussionData data)
        {
            var outcome = new VoteOutcome
            {
                MedianReductionTime = CalculateRecommendedReductionMedian(data),
                VoteReasons = data.DiscussionComments.SelectMany(c => c.CommentReasons).Distinct().ToList()
            };
            
            Console.WriteLine("Determining outcome based on the ban type...");

            if (data.BanType == "Role Ban")
            {
                Console.WriteLine("Ban type is Roleban.");
                outcome.VoteOutcomeType = DetermineRolebanOutcome(data);
            }
            else if (data.BanType == "Game Ban")
            {
                Console.WriteLine("Ban type is Gameban.");
                outcome.VoteOutcomeType = DetermineGamebanOutcome(data);
            }
            else
            {
                throw new ArgumentException("Invalid ban type");
            }

            return outcome;
        }

        private string DetermineRolebanOutcome(BanDiscussionData data)
        {
            var removeVotes = data.VoteOptions.Where(v => v.Type == "Remove").Sum(v => v.VoteCount);
            var maintainVotes = data.VoteOptions.Where(v => v.Type == "Maintain").Sum(v => v.VoteCount);
            var reduceVotes = data.VoteOptions.Where(v => v.Type == "Reduce").Sum(v => v.VoteCount);

            Console.WriteLine($"Remove Votes: {removeVotes}, Maintain Votes: {maintainVotes}, Reduce Votes: {reduceVotes}, Total Votes: {data.TotalVotes}");

            if (removeVotes > maintainVotes && removeVotes > reduceVotes && removeVotes > data.TotalVotes / 2)
            {
                return "Removal";
            }
            if (reduceVotes > maintainVotes && reduceVotes > data.TotalVotes / 2)
            {
                // Additional logic for reduction
                return "Reduction";
            }
            return "Maintain";
        }

        private string DetermineGamebanOutcome(BanDiscussionData data)
        {
            var voucherVotes = data.VoteOptions.Where(v => v.Type == "Upgrade").Sum(v => v.VoteCount);
            var removeVotes = data.VoteOptions.Where(v => v.Type == "Remove").Sum(v => v.VoteCount);
            var reduceVotes = data.VoteOptions.Where(v => v.Type == "Reduce").Sum(v => v.VoteCount);

            Console.WriteLine($"Voucher Votes: {voucherVotes}, Remove Votes: {removeVotes}, Reduce Votes: {reduceVotes}, Total Votes: {data.TotalVotes}");

            if (voucherVotes > (reduceVotes + removeVotes) && voucherVotes > 5 && voucherVotes > data.TotalVotes / 2)
            {
                return "Voucher";
            }
            if (removeVotes > reduceVotes && removeVotes > voucherVotes && removeVotes > data.TotalVotes / 2)
            {
                return "Removal";
            }
            if (reduceVotes > removeVotes && reduceVotes > voucherVotes && reduceVotes > data.TotalVotes / 2)
            {
                // Additional logic for reduction
                return "Reduction";
            }
            return "Maintain";
        }

        private int CalculateRecommendedReductionMedian(BanDiscussionData data)
        {
            // Filter out the comments with non-zero ReductionLengthTimeInWeeks
            var nonZeroReductionLengths = data.DiscussionComments
                .Where(comment => comment.ReductionLengthTimeInWeeks.HasValue && comment.ReductionLengthTimeInWeeks > 0)
                .Select(comment => comment.ReductionLengthTimeInWeeks.Value)
                .OrderBy(length => length)
                .ToList();

            if (nonZeroReductionLengths.Count == 0)
            {
                return DefaultReduction;
            }

            // Calculate median
            int count = nonZeroReductionLengths.Count;
            if (count % 2 == 0)
            {
                // Even number of elements, take the average of the two middle elements
                int midIndex = count / 2;
                return (nonZeroReductionLengths[midIndex - 1] + nonZeroReductionLengths[midIndex]) / 2;
            }
            else
            {
                // Odd number of elements, take the middle element
                int midIndex = count / 2;
                return nonZeroReductionLengths[midIndex];
            }
        }

    }
}
