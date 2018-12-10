// jsClipper uses X/Y instead of x/y...
function PlacementWorker(binPolygon, ids, rotations, config) {
    this._binPolygon = binPolygon;
    this._ids = ids;
    this._rotations = rotations;
    this._config = config;
    var _commons = new Commons();
    
    this.nfpCache = {};


    // return a placement for the paths/_rotations given
    // happens inside a webworker
    this.placePaths = function(paths) {
        if (!this._binPolygon) {
            return null;
        }

        var i, j, k, m, n, path;

        // rotate paths by given rotation
        var rotated = [];
        for (i = 0; i < paths.length; i++) {
            r = _commons.rotatePolygon(paths[i], paths[i].rotation);
            r.rotation = paths[i].rotation;
            r.source = paths[i].source;
            r.id = paths[i].id;
            rotated.push(r);
        }

        paths = rotated;

        var allplacements = [];
        var fitness = 0;
        var binarea = Math.abs(GeometryUtil.polygonArea(this._binPolygon));
        var key, nfp;

        while (paths.length > 0) {

            var placed = [];
            var placements = [];
            fitness += 1; // add 1 for each new bin opened (lower fitness is better)

            for (i = 0; i < paths.length; i++) {
                path = paths[i];

                // inner NFP
                key = JSON.stringify({
                    A: -1,
                    B: path.id,
                    inside: true,
                    Arotation: 0,
                    Brotation: path.rotation
                });
                var binNfp = this.nfpCache[key];

                // part unplaceable, skip
                if (!binNfp || binNfp.length == 0) {
                    continue;
                }

                // ensure all necessary NFPs exist
                var error = false;
                for (j = 0; j < placed.length; j++) {
                    key = JSON.stringify({
                        A: placed[j].id,
                        B: path.id,
                        inside: false,
                        Arotation: placed[j].rotation,
                        Brotation: path.rotation
                    });
                    nfp = this.nfpCache[key];

                    if (!nfp) {
                        error = true;
                        break;
                    }
                }

                // part unplaceable, skip
                if (error) {
                    continue;
                }

                var position = null;
                if (placed.length == 0) {
                    // first placement, put it on the left
                    for (j = 0; j < binNfp.length; j++) {
                        for (k = 0; k < binNfp[j].length; k++) {
                            if (position === null || binNfp[j][k].x - path[0].x < position.x) {
                                position = {
                                    x: binNfp[j][k].x - path[0].x,
                                    y: binNfp[j][k].y - path[0].y,
                                    id: path.id,
                                    rotation: path.rotation
                                }
                            }
                        }
                    }

                    placements.push(position);
                    placed.push(path);

                    continue;
                }

                var clipperBinNfp = [];
                for (j = 0; j < binNfp.length; j++) {
                    clipperBinNfp.push(_commons.toClipperCoordinates(binNfp[j]));
                }

                ClipperLib.JS.ScaleUpPaths(clipperBinNfp, this._config.clipperScale);

                var clipper = new ClipperLib.Clipper();
                var combinedNfp = new ClipperLib.Paths();


                for (j = 0; j < placed.length; j++) {
                    key = JSON.stringify({
                        A: placed[j].id,
                        B: path.id,
                        inside: false,
                        Arotation: placed[j].rotation,
                        Brotation: path.rotation
                    });
                    nfp = this.nfpCache[key];

                    if (!nfp) {
                        continue;
                    }

                    for (k = 0; k < nfp.length; k++) {
                        var clone = _commons.toClipperCoordinates(nfp[k]);
                        for (m = 0; m < clone.length; m++) {
                            clone[m].X += placements[j].x;
                            clone[m].Y += placements[j].y;
                        }

                        ClipperLib.JS.ScaleUpPath(clone, this._config.clipperScale);
                        clone = ClipperLib.Clipper.CleanPolygon(clone, 0.0001 * this._config.clipperScale);
                        var area = Math.abs(ClipperLib.Clipper.Area(clone));
                        if (clone.length > 2 && area > 0.1 * this._config.clipperScale * this._config.clipperScale) {
                            clipper.AddPath(clone, ClipperLib.PolyType.ptSubject, true);
                        }
                    }
                }

                if (!clipper.Execute(ClipperLib.ClipType.ctUnion, combinedNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero)) {
                    continue;
                }

                // difference with bin polygon
                var finalNfp = new ClipperLib.Paths();
                clipper = new ClipperLib.Clipper();

                clipper.AddPaths(combinedNfp, ClipperLib.PolyType.ptClip, true);
                clipper.AddPaths(clipperBinNfp, ClipperLib.PolyType.ptSubject, true);
                if (!clipper.Execute(ClipperLib.ClipType.ctDifference, finalNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero)) {
                    continue;
                }//WADDAFUCK

                finalNfp = ClipperLib.Clipper.CleanPolygons(finalNfp, 0.0001 * this._config.clipperScale);

                for (j = 0; j < finalNfp.length; j++) {
                    var area = Math.abs(ClipperLib.Clipper.Area(finalNfp[j]));
                    if (finalNfp[j].length < 3 || area < 0.1 * this._config.clipperScale * this._config.clipperScale) {
                        finalNfp.splice(j, 1);
                        j--;
                    }
                }

                if (!finalNfp || finalNfp.length == 0) {
                    continue;
                }

                var f = [];
                for (j = 0; j < finalNfp.length; j++) {
                    // back to normal scale
                    f.push(_commons.toNestCoordinates(finalNfp[j], this._config.clipperScale));
                }
                finalNfp = f;

                // choose placement that results in the smallest bounding box
                // could use convex hull instead, but it can create oddly shaped nests (triangles or long slivers) which are not optimal for real-world use
                // todo: generalize gravity direction
                var minwidth = null;
                var minarea = null;
                var minx = null;
                var nf, area, shiftvector;

                for (j = 0; j < finalNfp.length; j++) {
                    nf = finalNfp[j];
                    if (Math.abs(GeometryUtil.polygonArea(nf)) < 2) {
                        continue;
                    }

                    for (k = 0; k < nf.length; k++) {
                        var allpoints = [];
                        for (m = 0; m < placed.length; m++) {
                            for (n = 0; n < placed[m].length; n++) {
                                allpoints.push({
                                    x: placed[m][n].x + placements[m].x,
                                    y: placed[m][n].y + placements[m].y
                                });
                            }
                        }

                        shiftvector = {
                            x: nf[k].x - path[0].x,
                            y: nf[k].y - path[0].y,
                            id: path.id,
                            rotation: path.rotation,
                            nfp: combinedNfp
                        };

                        for (m = 0; m < path.length; m++) {
                            allpoints.push({
                                x: path[m].x + shiftvector.x,
                                y: path[m].y + shiftvector.y
                            });
                        }

                        var rectbounds = GeometryUtil.getPolygonBounds(allpoints);

                        // weigh width more, to help compress in direction of gravity
                        area = rectbounds.width * 2 + rectbounds.height;

                        if (minarea === null || area < minarea || (GeometryUtil.almostEqual(minarea, area) && (minx === null || shiftvector.x < minx))) {
                            minarea = area;
                            minwidth = rectbounds.width;
                            position = shiftvector;
                            minx = shiftvector.x;
                        }
                    }
                }
                if (position) {
                    placed.push(path);                    
                    placements.push(position);
                }
            }

            if (minwidth) {
                fitness += minwidth / binarea;
            }

            for (i = 0; i < placed.length; i++) {
                var index = paths.indexOf(placed[i]);
                if (index >= 0) {
                    paths.splice(index, 1);
                }
            }

            if (placements && placements.length > 0) {
                allplacements.push(placements);
            } else {
                break; // something went wrong
            }
        }

        // there were parts that couldn't be placed
        fitness += 2 * paths.length;

        return {
            placements: allplacements,
            fitness: fitness,
            paths: paths,
            area: binarea
        };
    };

}
