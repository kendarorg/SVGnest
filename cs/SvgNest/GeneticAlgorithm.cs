using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geometry;

namespace SvgNest
{
    public class GeneticAlgorithm
    {

        public GeneticAlgorithm(Commons commons,NestRandom random)
        {
            this.commons = commons;
            this._random = random;
        }
        private Commons commons;
        private SvgNestConfig config;
        private Rect binBounds;
        public List<Individual> population;

        // returns a random angle of insertion
        private double _randomAngle(Polygon part)
        {

            var angleList = new List<double>();
            for (var i = 0; i < Math.Max(this.config.rotations, 1); i++)
            {
                angleList.Add(i * (360 / this.config.rotations));
            }


            angleList = commons.shuffle(angleList);

            for (var i = 0; i < angleList.Count; i++)
            {
                var rotatedPart = GeometryUtil.RotatePolygon(part, angleList[i]);

                // don't use obviously bad angles where the part doesn't fit in the bin
                if (rotatedPart.Width < this.binBounds.Width && rotatedPart.Height < this.binBounds.Height)
                {
                    return angleList[i];
                }
            }

            return 0;
        }
        
        public void init(List<Polygon> adam, Polygon bin, SvgNestConfig config = null)
        {
            this.config = config ?? new SvgNestConfig
            {
                populationSize = 10,
                mutationRate = 10,
                rotations = 4
            };
            this.binBounds = GeometryUtil.GetPolygonBounds(bin);

            // population is an array of individuals. Each individual is a object representing the order of insertion and the angle each part is rotated
            var angles = new List<double>();
            for (var i = 0; i < adam.Count; i++)
            {
                angles.Add(this._randomAngle(adam[i]));
            }

            //_commons.log("GeneticAlgorithm init: ",angles);

            this.population = new List<Individual> {new Individual{
                Placements= adam,
                Rotations= angles


            }};

            //_commons.log("Mutation started", this.population.Count, config.populationSize);
            while (this.population.Count < config.populationSize)
            {
                var mutant = this._mutate(this.population[0]);
                this.population.Add(mutant);
            }

            //_commons.log("Mutation completed", this.population);
        }

        private int mutations = 0;

        private NestRandom _random;

        // returns a mutated individual with the given mutation rate
        private Individual _mutate(Individual individual)
        {
            var clone = new Individual
            {
                Placements = individual.Placements.slice(0),
                Rotations = individual.Rotations.slice(0)
            };
            for (var i = 0; i < clone.Placements.Count; i++)
            {
                var rand = _random.NextDouble();
                if (rand < 0.01 * (double)this.config.mutationRate)
                {
                    // swap current part with next part
                    var j = i + 1;

                    if (j < clone.Placements.Count)
                    {
                        var temp = clone.Placements[i];
                        clone.Placements[i] = clone.Placements[j];
                        clone.Placements[j] = temp;
                    }
                }

                rand = _random.NextDouble();
                if (rand < 0.01 * (double)this.config.mutationRate)
                {
                    clone.Rotations[i] = this._randomAngle(clone.Placements[i]);
                }
            }
            //commons.log("_mutate "+ mutations+" clone",clone);
            mutations++;
            return clone;
        }

        // returns a random individual from the population, weighted to the front of the list (lower fitness value is more likely to be selected)
        private Individual _randomWeightedIndividual(Individual exclude = null)
        {
            var pop = this.population.slice(0);

            if (exclude != null && pop.IndexOf(exclude) >= 0)
            {
                pop.splice(pop.IndexOf(exclude), 1);
            }

            var rand = _random.NextDouble();

            double lower = 0;
            double weight = 1 / (double)pop.Count;
            double upper = weight;

            for (var i = 0; i < pop.Count; i++)
            {
                // if the random number falls between lower and upper bounds, select this individual
                if (rand > lower && rand < upper)
                {
                    return pop[i];
                }
                lower = upper;
                upper += 2 * weight * (((double)pop.Count - (double)i) / (double)pop.Count);
            }

            return pop[0];
        }

        private bool _contains(List<Polygon> gene, int id)
        {
            for (var i = 0; i < gene.Count; i++)
            {
                if (gene[i].Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        // single point crossover
        private List<Individual> _mate(Individual male, Individual female)
        {
            var cutpoint =
                (int)Math.Round(Math.Min(Math.Max(_random.NextDouble(), 0.1), 0.9) * (male.Placements.Count - 1));

            var gene1 = male.Placements.slice(0, cutpoint);
            var rot1 = male.Rotations.slice(0, cutpoint);

            var gene2 = female.Placements.slice(0, cutpoint);
            var rot2 = female.Rotations.slice(0, cutpoint);

            var i = 0;

            for (i = 0; i < female.Placements.Count; i++)
            {
                if (!this._contains(gene1, female.Placements[i].Id))
                {
                    gene1.Add(female.Placements[i]);
                    rot1.Add(female.Rotations[i]);
                }
            }

            for (i = 0; i < male.Placements.Count; i++)
            {
                if (!this._contains(gene2, male.Placements[i].Id))
                {
                    gene2.Add(male.Placements[i]);
                    rot2.Add(male.Rotations[i]);
                }
            }

            return new List<Individual>
            {
               new Individual {
                    Placements= gene1,
                    Rotations= rot1
                }
            ,new Individual
            {
                Placements = gene2,
                Rotations = rot2
            }
        };
        }

        public void generation()
        {

            // Individuals with higher fitness are more likely to be selected for mating
            this.population.Sort((a, b) =>
            {
                if (a.Fitness == null && b.Fitness != null)
                {
                    return -1;
                }
                if (a.Fitness != null && a.Fitness == null)
                {
                    return 1;
                }
                if (b.Fitness == null && a.Fitness == null)
                {
                    return 0;
                }

                var result = a.Fitness.Value - b.Fitness.Value;
                return result > 0 ? 1 : (result < 0 ? -1 : 0);
            });

            // fittest individual is preserved in the new generation (elitism)
            var newpopulation = new List<Individual> { this.population[0] };

            while (newpopulation.Count < this.population.Count)
            {
                var male = this._randomWeightedIndividual();
                var female = this._randomWeightedIndividual(male);

                // each mating produces two children
                var children = this._mate(male, female);

                // slightly mutate children
                newpopulation.Add(this._mutate(children[0]));

                if (newpopulation.Count < this.population.Count)
                {
                    newpopulation.Add(this._mutate(children[1]));
                }
            }

            this.population = newpopulation;
        }
    }
}
