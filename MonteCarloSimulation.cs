using System.Collections.Concurrent;
using System.Text;

namespace KetoAnalyzer
{
    public static class MonteCarloSimulation
    {
        private static int[][] payoutScenarios = new int[11][];
        public static ConcurrentDictionary<int, int> payouts = new ConcurrentDictionary<int, int>();

        const int numberOfRunsPerScenario = 10_000_000;

        static MonteCarloSimulation()
        {
            for (int i = 0; i < payoutScenarios.Length; i++)
            {
                payoutScenarios[i] = new int[11];
            }

            payoutScenarios[1][1] = 2;

            payoutScenarios[2][2] = 10;

            payoutScenarios[3][3] = 25;
            payoutScenarios[3][2] = 2;

            payoutScenarios[4][4] = 50;
            payoutScenarios[4][3] = 5;
            payoutScenarios[4][2] = 1;

            payoutScenarios[5][5] = 500;
            payoutScenarios[5][4] = 15;
            payoutScenarios[5][3] = 2;

            payoutScenarios[6][6] = 1500;
            payoutScenarios[6][5] = 50;
            payoutScenarios[6][4] = 5;
            payoutScenarios[6][3] = 1;

            payoutScenarios[7][7] = 5000;
            payoutScenarios[7][6] = 150;
            payoutScenarios[7][5] = 15;
            payoutScenarios[7][4] = 2;
            payoutScenarios[7][3] = 1;

            payoutScenarios[8][8] = 15000;
            payoutScenarios[8][7] = 400;
            payoutScenarios[8][6] = 50;
            payoutScenarios[8][5] = 10;
            payoutScenarios[8][4] = 2;

            payoutScenarios[9][9] = 25000;
            payoutScenarios[9][8] = 2500;
            payoutScenarios[9][7] = 200;
            payoutScenarios[9][6] = 25;
            payoutScenarios[9][5] = 4;
            payoutScenarios[9][4] = 1;

            payoutScenarios[10][10] = 200000;
            payoutScenarios[10][9] = 10000;
            payoutScenarios[10][8] = 500;
            payoutScenarios[10][7] = 50;
            payoutScenarios[10][6] = 10;
            payoutScenarios[10][5] = 3;
            payoutScenarios[10][0] = 3;
        }

        public static StringBuilder RunSimulation(StringBuilder latexBuilder)
        {
            var partitioner = Partitioner.Create(1, 11);
            Parallel.ForEach(partitioner, i =>
            {
                var localRandom = new Random(Guid.NewGuid().GetHashCode());

                var localPayouts = new Dictionary<int, int>();

                for (var r = 0; r < numberOfRunsPerScenario; r++)
                {
                    var randomPicks = GenerateRandomNumbers(localRandom, i.Item1, 1, 80);

                    var randomDraw = GenerateRandomNumbers(localRandom, 20, 1, 80);

                    var matches = CountMatches(randomPicks, randomDraw);

                    var prize = GetPrize(matches, i.Item1);

                    if (!localPayouts.ContainsKey(i.Item1))
                        localPayouts[i.Item1] = 0;
                    localPayouts[i.Item1] += prize;
                }

                lock (payouts)
                {
                    foreach (var kvp in localPayouts)
                    {
                        if (!payouts.ContainsKey(kvp.Key))
                            payouts[kvp.Key] = 0;
                        payouts[kvp.Key] += kvp.Value;
                    }
                }
            });

            latexBuilder.AppendLine(@"\vspace{1cm}");
            latexBuilder.AppendLine(@"\begin{center}");
            latexBuilder.AppendLine(@"\textbf{Monte Carlo Simulation Results}");
            latexBuilder.AppendLine(@"\end{center}");
            latexBuilder.AppendLine(@"\begin{center}");
            latexBuilder.AppendLine(@"\textbf{(10,000,000 Runs x pick scenario)}");
            latexBuilder.AppendLine(@"\end{center}");
            latexBuilder.AppendLine(@"\begin{center}");
            latexBuilder.AppendLine(@"\begin{tabular}{cc}");
            latexBuilder.AppendLine(@"\toprule");
            latexBuilder.AppendLine(@"Pick & Prize Amount \\");
            latexBuilder.AppendLine(@"\midrule");

            foreach (var kv in payouts)
            {
                latexBuilder.AppendLine($"{kv.Key} & {kv.Value}\\\\");
            }

            latexBuilder.AppendLine(@"\bottomrule");
            latexBuilder.AppendLine(@"\end{tabular}");
            latexBuilder.AppendLine(@"\end{center}");

            latexBuilder.AppendLine(@"\end{document}");

            return latexBuilder;
        }

        private static int[] GenerateRandomNumbers(Random random, int count, int min, int max)
        {
            var numbers = new HashSet<int>();
            while (numbers.Count < count)
            {
                numbers.Add(random.Next(min, max + 1));
            }
            return numbers.ToArray();
        }

        private static int CountMatches(int[] playerPicks, int[] kenoDraw)
        {
            return playerPicks.Intersect(kenoDraw).Count();
        }

        private static int GetPrize(int numberOfMatches, int picks)
        {
            return payoutScenarios[picks][numberOfMatches];
        }

    }

}
