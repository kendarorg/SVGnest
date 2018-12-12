/*!
 * SvgNest
 * Licensed under the MIT license
 */

(function(root) {
    'use strict';

    root.SvgNest = new SvgNest();

    function SvgNest() {
        var _commons = new Commons();

        var _svg = null;

        // keep a reference to any style nodes, to maintain color/fill info
        
        this.style = null;

        var _parts = null;

        var _tree = null;


        var _individual = null;
        var _bin = null;
        var _binPolygon = null;
        var _binBounds = null;
        var _nfpCache = {};
        var _config = {
            clipperScale: 10000000,
            curveTolerance: 0.3,
            spacing: 1,
            rotations: 4,
            populationSize: 10,
            mutationRate: 10,
            useHoles: false,
            exploreConcave: false,
            iterations: 4,
            searchEdges:true
        };




        this.working = false;

		var _displayCallback = function(percent){
			_commons.log(percent);
		}
		var _progressCallback = function renderSvg(svglist, efficiency, numplaced){
			_commons.log("Rendering svg numplaced:"+numplaced);
		}
        var _geneticAlgorithm = null;
        var _best = null;
        
        var _progress = 0;

		
        this.parsesvg = function(svgstring) {
            // reset if in _progress
            this.stop();

            _bin = null;
            _binPolygon = null;
            _tree = null;

            // parse _svg
            _svg = SvgParser.load(svgstring);

            this.style = SvgParser.getStyle();

            _svg = SvgParser.clean();

            _tree = this._getParts(_svg.children);

            return _svg;
        }
        
		
        this.setbin = function(element) {
            if (!_svg) {
                return;
            }
            _bin = element;
        }

		
        this.config = function(c) {
            // clean up inputs

            if (!c) {
                return _config;
            }

            if (c.curveTolerance && !GeometryUtil.almostEqual(parseFloat(c.curveTolerance), 0)) {
                _config.curveTolerance = parseFloat(c.curveTolerance);
            }

            if (c.spacing!==undefined) {
                _config.spacing = parseFloat(c.spacing);
            }

            if (c.rotations && parseInt(c.rotations) > 0) {
                _config.rotations = parseInt(c.rotations);
            }

            if (c.populationSize && parseInt(c.populationSize) > 2) {
                _config.populationSize = parseInt(c.populationSize);
            }

            if (c.mutationRate && parseInt(c.mutationRate) > 0) {
                _config.mutationRate = parseInt(c.mutationRate);
            }

            if (c.useHoles!==undefined) {
                _config.useHoles = !!c.useHoles;
            }

            if (c.exploreConcave!==undefined) {
                _config.exploreConcave = !!c.exploreConcave;
            }

            SvgParser.config({
                tolerance: _config.curveTolerance
            });

            _best = null;
            _nfpCache = {};
            _binPolygon = null;
            _geneticAlgorithm = null;

            return _config;
        }

		var startedAlready=false;
		
		this.preparePolygons= function(progressCallback, displayCallback) {
			if(progressCallback){
            	_progressCallback = progressCallback;
            }
            if(displayCallback){
            	_displayCallback = displayCallback;
            }

            //_parts = Array.prototype.slice.call(_svg.children);
            _parts = [];
            for(var x=0;x<_svg.children.length;x++){
            	_parts.push(_svg.children[x]);
            }
            
            var binindex = _parts.indexOf(_bin);

            if (binindex >= 0) {
                // don't process bin as a part of the _tree
                _parts.splice(binindex, 1);
            }

            // build _tree without bin
            _tree = this._getParts(_parts.slice(0));

            this._offsetTree(_tree, 0.5 * _config.spacing);



            _binPolygon = SvgParser.polygonify(_bin);
            _binPolygon = this._cleanPolygon(_binPolygon);

            if (!_binPolygon || _binPolygon.length < 3) {
                return false;
            }

            _binBounds = GeometryUtil.getPolygonBounds(_binPolygon);

            if (_config.spacing > 0) {
                var offsetBin = this._polygonOffset(_binPolygon, -0.5 * _config.spacing);
                if (offsetBin.length == 1) {
                    // if the offset contains 0 or more than 1 path, something went wrong.
                    _binPolygon = offsetBin.pop();
                }
            }

            _binPolygon.id = -1;

            // put bin on origin
            var xbinmax = _binPolygon[0].x;
            var xbinmin = _binPolygon[0].x;
            var ybinmax = _binPolygon[0].y;
            var ybinmin = _binPolygon[0].y;

            for (var i = 1; i < _binPolygon.length; i++) {
                if (_binPolygon[i].x > xbinmax) {
                    xbinmax = _binPolygon[i].x;
                } else if (_binPolygon[i].x < xbinmin) {
                    xbinmin = _binPolygon[i].x;
                }
                if (_binPolygon[i].y > ybinmax) {
                    ybinmax = _binPolygon[i].y;
                } else if (_binPolygon[i].y < ybinmin) {
                    ybinmin = _binPolygon[i].y;
                }
            }

            for (i = 0; i < _binPolygon.length; i++) {
                _binPolygon[i].x -= xbinmin;
                _binPolygon[i].y -= ybinmin;
            }

            _binPolygon.width = xbinmax - xbinmin;
            _binPolygon.height = ybinmax - ybinmin;

            // all paths need to have the same winding direction
            if (GeometryUtil.polygonArea(_binPolygon) > 0) {
                _binPolygon.reverse();
            }

            // remove duplicate endpoints, ensure counterclockwise winding direction
            for (i = 0; i < _tree.length; i++) {
                var start = _tree[i][0];
                var end = _tree[i][_tree[i].length - 1];
                if (start == end || (GeometryUtil.almostEqual(start.x, end.x) && GeometryUtil.almostEqual(start.y, end.y))) {
                    _tree[i].pop();
                }

                if (GeometryUtil.polygonArea(_tree[i]) > 0) {
                    _tree[i].reverse();
                }
            }
            return true;
		}
		
        // progressCallback is called when _progress is made
        // displayCallback is called when a new placement has been made
        this.start = function(progressCallback, displayCallback) {
        	if(startedAlready)return false;
        	startedAlready=true;
            if (!_svg || !_bin) {
                return false;
            }
            if (!this.preparePolygons(progressCallback, displayCallback)) return null;

            this.working = false;
            _commons.log("Before _launchWorkers ",_tree,_binPolygon);
            this._launchWorkers(_tree,_binPolygon);
            this.working = true;
            _progressCallback(_progress);
        };

       	
        this.stop = function() {
            this.working = false;
        };

        this._minkowskiDifference = function(A, B) {
            var Ac = _commons.toClipperCoordinates(A);
            ClipperLib.JS.ScaleUpPath(Ac, 10000000);
            var Bc = _commons.toClipperCoordinates(B);
            ClipperLib.JS.ScaleUpPath(Bc, 10000000);
            for (var i = 0; i < Bc.length; i++) {
                Bc[i].X *= -1;
                Bc[i].Y *= -1;
            }
            var solution = ClipperLib.Clipper.MinkowskiSum(Ac, Bc, true);
            var clipperNfp;

            var largestArea = null;
            for (i = 0; i < solution.length; i++) {
                var n = _commons.toNestCoordinates(solution[i], 10000000);
                var sarea = GeometryUtil.polygonArea(n);
                if (largestArea === null || largestArea > sarea) {
                    clipperNfp = n;
                    largestArea = sarea;
                }
            }

            for (var i = 0; i < clipperNfp.length; i++) {
                clipperNfp[i].x += B[0].x;
                clipperNfp[i].y += B[0].y;
            }

            return [clipperNfp];
        };


        // offset _tree recursively
        this._offsetTree = function(t, offset) {
            for (var i = 0; i < t.length; i++) {
                var offsetpaths = this._polygonOffset(t[i], offset);
                
                if (offsetpaths.length == 1) {
                
                    // replace array items in place
                    Array.prototype.splice.apply(t[i], [0, t[i].length].concat(offsetpaths[0]));
                }

                if (t[i].children && t[i].children.length > 0) {
                    this._offsetTree(t[i].children, -offset);
                }
            }
        }
        
        this.generateNfpForInside = function(pair, A, B, searchEdges){
        	var nfp = null;
        	if (GeometryUtil.isRectangle(A, 0.001)) {
                nfp = GeometryUtil.noFitPolygonRectangle(A, B);
            } else {
                nfp = GeometryUtil.noFitPolygon(A, B, true, searchEdges);
            }

            // ensure all interior NFPs have the same winding direction
            if (nfp && nfp.length > 0) {
                for (var i = 0; i < nfp.length; i++) {
                    if (GeometryUtil.polygonArea(nfp[i]) > 0) {
                        nfp[i].reverse();
                    }
                }
            } else {
                // warning on null inner NFP
                // this is not an error, as the part may simply be larger than the bin or otherwise unplaceable due to geometry
                _commons.log('NFP Warning: ', pair.key);
            }
            return nfp;
        }

       this._functionPair=function(pair) {
            if (!pair || pair.length == 0) {
                return null;
            }
            var searchEdges = _config.searchEdges;
            var useHoles = _config.useHoles;

            var A = _commons.rotatePolygon(pair.A, pair.key.Arotation);
            var B = _commons.rotatePolygon(pair.B, pair.key.Brotation);

            var nfp;

            if (pair.key.inside) {
                nfp = this.generateNfpForInside(pair, A, B, searchEdges);
            } else {
            	nfp = this.generateStandadNfp(pair, searchEdges, A, B);
                if(nfp==null)return null;

                // generate nfps for children (holes of _parts) if any exist
                if (useHoles && A.children && A.children.length > 0) {
                	this.generateNfpWithHoles(B, A, searchEdges, nfp);
                    
                }
            }



            return {
                key: pair.key,
                value: nfp
            };
        }
        
        this.generateStandadNfp=function(pair, searchEdges, A, B){
        	var nfp = [];
        	if (searchEdges) {
                    nfp = GeometryUtil.noFitPolygon(A, B, false, searchEdges);
                } else {
                    nfp = this._minkowskiDifference(A, B);
                }
                // sanity check
                if (!nfp || nfp.length == 0) {
                    _commons.log('NFP Error: ', pair.key);
                    _commons.log('A: ', JSON.stringify(A));
                    _commons.log('B: ', JSON.stringify(B));
                    return null;
                }

                for (var i = 0; i < nfp.length; i++) {
                    if (!searchEdges || i == 0) { // if searchedges is active, only the first NFP is guaranteed to pass sanity check
                        if (Math.abs(GeometryUtil.polygonArea(nfp[i])) < Math.abs(GeometryUtil.polygonArea(A))) {
                            _commons.log('NFP Area Error: ', Math.abs(GeometryUtil.polygonArea(nfp[i])), pair.key);
                            _commons.log('NFP:', JSON.stringify(nfp[i]));
                            _commons.log('A: ', JSON.stringify(A));
                            _commons.log('B: ', JSON.stringify(B));
                            nfp.splice(i, 1);
                            return null;
                        }
                    }
                }

                if (nfp.length == 0) {
                    return null;
                }

                // for outer NFPs, the first is guaranteed to be the largest. Any subsequent NFPs that lie inside the first are holes
                for (var i = 0; i < nfp.length; i++) {
                    if (GeometryUtil.polygonArea(nfp[i]) > 0) {
                        nfp[i].reverse();
                    }

                    if (i > 0) {
                        if (GeometryUtil.pointInPolygon(nfp[i][0], nfp[0])) {
                            if (GeometryUtil.polygonArea(nfp[i]) < 0) {
                                nfp[i].reverse();
                            }
                        }
                    }
                }
                return nfp;
        }
        
        this.generateNfpWithHoles = function(B, A, searchEdges, nfp){
        	var Bbounds = GeometryUtil.getPolygonBounds(B);

            for (var i = 0; i < A.children.length; i++) {
                var Abounds = GeometryUtil.getPolygonBounds(A.children[i]);

                // no need to find nfp if B's bounding box is too big
                if (Abounds.width > Bbounds.width && Abounds.height > Bbounds.height) {

                    var cnfp = GeometryUtil.noFitPolygon(A.children[i], B, true, searchEdges);
                    // ensure all interior NFPs have the same winding direction
                    if (cnfp && cnfp.length > 0) {
                        for (var j = 0; j < cnfp.length; j++) {
                            if (GeometryUtil.polygonArea(cnfp[j]) < 0) {
                                cnfp[j].reverse();
                            }
                            nfp.push(cnfp[j]);
                        }
                    }

                }
            }
        }

        this._functionPlacements = function(placements, placelist) {
            if (!placements || placements.length == 0) {
                return;
            }

            _individual.fitness = placements[0].fitness;
            var bestresult = placements[0];

            for (var i = 1; i < placements.length; i++) {
                if (placements[i].fitness < bestresult.fitness) {
                    bestresult = placements[i];
                }
            }

            if (!_best || bestresult.fitness < _best.fitness) {
                _best = bestresult;

                var placedArea = 0;
                var totalArea = 0;
                var numParts = placelist.length;
                var numPlacedParts = 0;

                for (i = 0; i < _best.placements.length; i++) {
                    totalArea += Math.abs(GeometryUtil.polygonArea(_binPolygon));
                    for (var j = 0; j < _best.placements[i].length; j++) {
                        placedArea += Math.abs(GeometryUtil.polygonArea(_tree[_best.placements[i][j].id]));
                        numPlacedParts++;
                    }
                }
                _displayCallback(this._applyPlacement(_best.placements), placedArea / totalArea, numPlacedParts + '/' + numParts);
            } else {
                _displayCallback(0);
            }
            this.working = false;
        }

        this._functionGeneratedNfp = function(generatedNfp, worker, placelist) {
            if (generatedNfp) {
                for (var i = 0; i < generatedNfp.length; i++) {
                    var Nfp = generatedNfp[i];

                    if (Nfp) {
                        // a null nfp means the nfp could not be generated, either because the _parts simply don't fit or an error in the nfp algo
                        var key = JSON.stringify(Nfp.key);
                        _nfpCache[key] = Nfp.value;
                    }
                }
            }
            worker.nfpCache = _nfpCache;


            var placements = [];
            var placelistsliced = [placelist.slice(0)];
            	
            try {
                for (var i = 0; i < placelistsliced.length; i++) {
                    placements.push(worker.placePaths(placelistsliced[i]));
                }

                this._functionPlacements(placements, placelist);
                return placements;
            } catch (err) {
                _commons.log(err);
            }
			return null;
        };
        
        this._launchWorkers = function(treeLocal,binPolygonLocal) {
            if (_geneticAlgorithm === null) {
                // initiate new _geneticAlgorithm
                var adam = treeLocal.slice(0);

                // seed with decreasing area
                adam.sort(function(a, b) {
                    return Math.abs(GeometryUtil.polygonArea(b)) - Math.abs(GeometryUtil.polygonArea(a));
                });

                _geneticAlgorithm = new GeneticAlgorithm();
                _geneticAlgorithm.init(adam, binPolygonLocal, _config);
            }



            // evaluate all members of the population
            for (var i = 0; i < _geneticAlgorithm.population.length; i++) {
                if (!_geneticAlgorithm.population[i].fitness) {
                    _individual = _geneticAlgorithm.population[i];
                    break;
                }
            }

            if (_individual === null) {
                // all individuals have been evaluated, start next generation
                _geneticAlgorithm.generation();
                _individual = _geneticAlgorithm.population[1];
            }

            var placelist = _individual.placement;
            var rotations = _individual.rotation;

            var ids = [];
            for (var i = 0; i < placelist.length; i++) {
                ids.push(placelist[i].id);
                placelist[i].rotation = rotations[i];
            }

			_commons.log("Placelist _launchWorkers ",placelist,rotations);
            var nfpPairs = [];
            var key;
            var newCache = {};

            for (var i = 0; i < placelist.length; i++) {
                var part = placelist[i];
                key = {
                    A: binPolygonLocal.id,
                    B: part.id,
                    inside: true,
                    Arotation: 0,
                    Brotation: rotations[i]
                };
                if (!_nfpCache[JSON.stringify(key)]) {
                    nfpPairs.push({
                        A: binPolygonLocal,
                        B: part,
                        key: key
                    });
                } else {
                    newCache[JSON.stringify(key)] = _nfpCache[JSON.stringify(key)]
                }
                for (var j = 0; j < i; j++) {
                    var placed = placelist[j];
                    key = {
                        A: placed.id,
                        B: part.id,
                        inside: false,
                        Arotation: rotations[j],
                        Brotation: rotations[i]
                    };
                    if (!_nfpCache[JSON.stringify(key)]) {
                        nfpPairs.push({
                            A: placed,
                            B: part,
                            key: key
                        });
                    } else {
                        newCache[JSON.stringify(key)] = _nfpCache[JSON.stringify(key)]
                    }
                }
            }

            // only keep cache for one cycle
            _nfpCache = newCache;
            
            return this.generatePlacements(binPolygonLocal, ids, rotations, nfpPairs, placelist);

            
        }
        
        this.generatePlacements=function(binPolygonLocal, ids, rotations, nfpPairs, placelist){
        	var worker = new PlacementWorker(binPolygonLocal, ids, rotations, _config);
            
            var spawncount = 0;

            var generatedNfp = [];
            var generatedPlacements = [];
            for (var w = 0; w < _config.iterations; w++) {
                try {
                	
                    for (var i = 0; i < nfpPairs.length; i++) {
                        generatedNfp.push(this._functionPair(nfpPairs[i]));
                    }

                    generatedPlacements.push(this._functionGeneratedNfp(generatedNfp, worker, placelist));
                    _progress = spawncount++/nfpPairs.length;
                    _commons.log(_progress);
                } catch (err) {
                    _commons.log(err);
                }
            }
        }

        this._toTree = function(list, idstart) {
            var parents = [];
            var i, j;

            // assign a unique id to each leaf
            var id = idstart || 0;

            for (i = 0; i < list.length; i++) {
                var p = list[i];

                var ischild = false;
                for (j = 0; j < list.length; j++) {
                    if (j == i) {
                        continue;
                    }
                    if (GeometryUtil.pointInPolygon(p[0], list[j]) === true) {
                        if (!list[j].children) {
                            list[j].children = [];
                        }
                        list[j].children.push(p);
                        p.parent = list[j];
                        ischild = true;
                        break;
                    }
                }

                if (!ischild) {
                    parents.push(p);
                }
            }

            for (i = 0; i < list.length; i++) {
                if (parents.indexOf(list[i]) < 0) {
                    list.splice(i, 1);
                    i--;
                }
            }

            for (i = 0; i < parents.length; i++) {
                parents[i].id = id;
                id++;
            }

            for (i = 0; i < parents.length; i++) {
                if (parents[i].children) {
                    id = this._toTree(parents[i].children, id);
                }
            }

            return id;
        }
        
        // assuming no intersections, return a _tree where odd leaves are _parts and even ones are holes
        // might be easier to use the DOM, but paths can't have paths as children. So we'll just make our own _tree.
        this._getParts = function(paths) {
            var i, j;
            var polygons = [];

            var numChildren = paths.length;
            for (i = 0; i < numChildren; i++) {
                var poly = SvgParser.polygonify(paths[i]);
                poly = this._cleanPolygon(poly);

                // todo: warn user if poly could not be processed and is excluded from the nest
                if (poly && poly.length > 2 && Math.abs(GeometryUtil.polygonArea(poly)) > _config.curveTolerance * _config.curveTolerance) {
                    poly.source = i;
                    polygons.push(poly);
                }
            }

            // turn the list into a _tree
            this._toTree(polygons);

            return polygons;
        };

        // use the clipper library to return an offset to the given polygon. Positive offset expands the polygon, negative contracts
        // note that this returns an array of polygons
        this._polygonOffset = function(polygon, offset) {
            if (!offset || offset == 0 || GeometryUtil.almostEqual(offset, 0)) {
                return polygon;
            }

            var p = this._svgToClipper(polygon);

            var miterLimit = 2;
            var co = new ClipperLib.ClipperOffset(miterLimit, _config.curveTolerance * _config.clipperScale);
            co.AddPath(p, ClipperLib.JoinType.jtRound, ClipperLib.EndType.etClosedPolygon);

            var newpaths = new ClipperLib.Paths();
            co.Execute(newpaths, offset * _config.clipperScale);

            var result = [];
            for (var i = 0; i < newpaths.length; i++) {
                result.push(this._clipperToSvg(newpaths[i]));
            }

            return result;
        };

        // returns a less complex polygon that satisfies the curve tolerance
        this._cleanPolygon = function(polygon) {
            var p = this._svgToClipper(polygon);
            // remove self-intersections and find the biggest polygon that's left
            var simple = ClipperLib.Clipper.SimplifyPolygon(p, ClipperLib.PolyFillType.pftNonZero);

            if (!simple || simple.length == 0) {
                return null;
            }

            var biggest = simple[0];
            var biggestarea = Math.abs(ClipperLib.Clipper.Area(biggest));
            for (var i = 1; i < simple.length; i++) {
                var area = Math.abs(ClipperLib.Clipper.Area(simple[i]));
                if (area > biggestarea) {
                    biggest = simple[i];
                    biggestarea = area;
                }
            }

            // clean up singularities, coincident points and edges
            var clean = ClipperLib.Clipper.CleanPolygon(biggest, _config.curveTolerance * _config.clipperScale);

            if (!clean || clean.length == 0) {
                return null;
            }

            return this._clipperToSvg(clean);
        }

        // converts a polygon from normal float coordinates to integer coordinates used by clipper, as well as x/y -> X/Y
        this._svgToClipper = function(polygon) {
            var clip = [];
            for (var i = 0; i < polygon.length; i++) {
                clip.push({
                    X: polygon[i].x,
                    Y: polygon[i].y
                });
            }

            ClipperLib.JS.ScaleUpPath(clip, _config.clipperScale);

            return clip;
        }

        this._clipperToSvg = function(polygon) {
            var normal = [];

            for (var i = 0; i < polygon.length; i++) {
                normal.push({
                    x: polygon[i].X / _config.clipperScale,
                    y: polygon[i].Y / _config.clipperScale
                });
            }

            return normal;
        }

        // returns an array of SVG elements that represent the placement, for export or rendering
        this._applyPlacement = function(placement) {
            var i, j, k;
            var clone = [];
            //create a copy of the list of items
            for (i = 0; i < _parts.length; i++) {
                clone.push(_parts[i].cloneNode(false));
            }

            var svglist = [];

			//foreach blocks of placements (one for each destination bin)
            for (i = 0; i < placement.length; i++) {
            	//create a new _svg container
                var newsvg = _svg.cloneNode(false);
                newsvg.setAttribute('viewBox', '0 0 ' + _binBounds.width + ' ' + _binBounds.height);
                newsvg.setAttribute('width', _binBounds.width + 'px');
                newsvg.setAttribute('height', _binBounds.height + 'px');
                //create a new box to contain the data
                var binclone = _bin.cloneNode(false);

                binclone.setAttribute('class', 'bin');
                //set it in 0,0 position
                binclone.setAttribute('transform', 'translate(' + (-_binBounds.x) + ' ' + (-_binBounds.y) + ')');
                
                //add the box to the list of items
                newsvg.appendChild(binclone);

				//add each polygon to the box
                for (j = 0; j < placement[i].length; j++) {
                    var p = placement[i][j];
                    var part = _tree[p.id];

                    // the original path could have transforms and stuff on it, so apply our transforms on a group
                    var partgroup = document.createElementNS(_svg.namespaceURI, 'g');
                    //show it rotated and offseted
                    partgroup.setAttribute('transform', 'translate(' + p.x + ' ' + p.y + ') rotate(' + p.rotation + ')');
                    partgroup.appendChild(clone[part.source]);

                    if (part.children && part.children.length > 0) {
                        var flattened = this._flattenTree(part.children, true);
                        for (k = 0; k < flattened.length; k++) {

                            var c = clone[flattened[k].source];
                            // add class to indicate hole
                            if (flattened[k].hole && (!c.getAttribute('class') || c.getAttribute('class').indexOf('hole') < 0)) {
                                c.setAttribute('class', c.getAttribute('class') + ' hole');
                            }
                            partgroup.appendChild(c);
                        }
                    }

                    newsvg.appendChild(partgroup);
                }

                svglist.push(newsvg);
            }



            return svglist;
        }

        // flatten the given _tree into a list
        this._flattenTree = function(t, hole) {
            var flat = [];
            for (var i = 0; i < t.length; i++) {
                flat.push(t[i]);
                t[i].hole = hole;
                if (t[i].children && t[i].children.length > 0) {
                    flat = flat.concat(_flattenTree(t[i].children, !hole));
                }
            }

            return flat;
        }
    }

})(this);