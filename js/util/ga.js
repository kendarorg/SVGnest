//xx
function GeneticAlgorithm() {

    var commons = new Commons();
    
    //xx
    this.generation = function() {

        // Individuals with higher fitness are more likely to be selected for mating
        this.population.sort(function(a, b) {
            return a.fitness - b.fitness;
        });

        // fittest individual is preserved in the new generation (elitism)
        var newpopulation = [this.population[0]];

        while (newpopulation.length < this.population.length) {
            var male = this._randomWeightedIndividual();
            var female = this._randomWeightedIndividual(male);

            // each mating produces two children
            var children = this._mate(male, female);

            // slightly mutate children
            newpopulation.push(this._mutate(children[0]));

            if (newpopulation.length < this.population.length) {
                newpopulation.push(this._mutate(children[1]));
            }
        }

        this.population = newpopulation;
    }
    
    
    
    this.init=function(adam, bin, config){

	    this.config = config || {
	        populationSize: 10,
	        mutationRate: 10,
	        rotations: 4
	    };
	    this.binBounds = GeometryUtil.getPolygonBounds(bin);

	    // population is an array of individuals. Each individual is a object representing the order of insertion and the angle each part is rotated
	    var angles = [];
	    for (var i = 0; i < adam.length; i++) {
	        angles.push(this._randomAngle(adam[i]));
	    }
	    
	    this.population = [{
	        placement: adam,
	        rotation: angles
	    }];

	    while (this.population.length < config.populationSize) {
	        var mutant = this._mutate(this.population[0]);
	        this.population.push(mutant);
	    }
	}
	
    //xx
    // returns a random angle of insertion
    this._randomAngle = function(part) {

        var angleList = [];
        for (var i = 0; i < Math.max(this.config.rotations, 1); i++) {
            angleList.push(i * (360 / this.config.rotations));
        }


        angleList = commons.shuffle(angleList);

        for (i = 0; i < angleList.length; i++) {
            var rotatedPart = GeometryUtil.rotatePolygon(part, angleList[i]);

            // don't use obviously bad angles where the part doesn't fit in the bin
            if (rotatedPart.width < this.binBounds.width && rotatedPart.height < this.binBounds.height) {
                return angleList[i];
            }
        }

        return 0;
    }

    //xx
    // returns a mutated individual with the given mutation rate
    this._mutate = function(individual) {
        var clone = {
            placement: individual.placement.slice(0),
            rotation: individual.rotation.slice(0)
        };
        for (var i = 0; i < clone.placement.length; i++) {
            var rand = NestRandom.random();
            if (rand < 0.01 * this.config.mutationRate) {
                // swap current part with next part
                var j = i + 1;

                if (j < clone.placement.length) {
                    var temp = clone.placement[i];
                    clone.placement[i] = clone.placement[j];
                    clone.placement[j] = temp;
                }
            }

            rand = NestRandom.random();
            if (rand < 0.01 * this.config.mutationRate) {
                clone.rotation[i] = this._randomAngle(clone.placement[i]);
            }
        }

        return clone;
    }

    // single point crossover
    this._mate = function(male, female) {
        var cutpoint = Math.round(Math.min(Math.max(NestRandom.random(), 0.1), 0.9) * (male.placement.length - 1));

        var gene1 = male.placement.slice(0, cutpoint);
        var rot1 = male.rotation.slice(0, cutpoint);

        var gene2 = female.placement.slice(0, cutpoint);
        var rot2 = female.rotation.slice(0, cutpoint);

        var i;

        for (i = 0; i < female.placement.length; i++) {
            if (!this._contains(gene1, female.placement[i].id)) {
                gene1.push(female.placement[i]);
                rot1.push(female.rotation[i]);
            }
        }

        for (i = 0; i < male.placement.length; i++) {
            if (!this._contains(gene2, male.placement[i].id)) {
                gene2.push(male.placement[i]);
                rot2.push(male.rotation[i]);
            }
        }



        return [{
            placement: gene1,
            rotation: rot1
        }, {
            placement: gene2,
            rotation: rot2
        }];
    }

    this._contains = function(gene, id) {
        for (var i = 0; i < gene.length; i++) {
            if (gene[i].id == id) {
                return true;
            }
        }
        return false;
    }

    // returns a random individual from the population, weighted to the front of the list (lower fitness value is more likely to be selected)
    this._randomWeightedIndividual = function(exclude) {
        var pop = this.population.slice(0);

        if (exclude && pop.indexOf(exclude) >= 0) {
            pop.splice(pop.indexOf(exclude), 1);
        }

        var rand = NestRandom.random();

        var lower = 0;
        var weight = 1 / pop.length;
        var upper = weight;

        for (var i = 0; i < pop.length; i++) {
            // if the random number falls between lower and upper bounds, select this individual
            if (rand > lower && rand < upper) {
                return pop[i];
            }
            lower = upper;
            upper += 2 * weight * ((pop.length - i) / pop.length);
        }

        return pop[0];
    };

    
}