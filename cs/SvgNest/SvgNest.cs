using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using Geometry;
using Newtonsoft.Json;

namespace SvgNest
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class SvgNest
    {
        private Dictionary<string, List<Polygon>> _nfpCache = new Dictionary<string, List<Polygon>>();
        private Commons _commons = new Commons();
        private SvgNestConfig _config = new SvgNestConfig();
        private GeneticAlgorithm _geneticAlgorithm;
        private double _progress = 0;
        private List<Polygon> _svg;
        private List<Polygon> _tree;

        public bool working { get; set; }

        private Action<object> _progressCallback = Console.WriteLine;
        private Action<object,object,object> _displayCallback = (a, b, c) => { };
        private Polygon _bin;
        private List<Polygon> _parts;
        private Polygon _binPolygon;
        private Individual _individual;
        private Rect _binBounds;
        private Placement _best;

        // converts a polygon from normal float coordinates to integer coordinates used by clipper, as well as x/y -> X/Y
        private Path _svgToClipper(Polygon polygon)
        {
            var clip = new Path();
            for (var i = 0; i < polygon.Count; i++)
            {
                clip.Add(new IntPoint
                {
                    X = (long)polygon[i].X,
                    Y = (long)polygon[i].Y
                });
            }

            Clipper.ScaleUpPath(clip, _config.clipperScale);

            return clip;
        }


        private Polygon _clipperToSvg(Path polygon)
        {
            var normal = new Polygon();

            for (var i = 0; i < polygon.Count; i++)
            {
                normal.Add(new Point
                {
                    X = polygon[i].X / _config.clipperScale,
                    Y = polygon[i].Y / _config.clipperScale
                });
            }

            return normal;
        }

        // returns a less complex polygon that satisfies the curve tolerance
        private Polygon _cleanPolygon(Polygon polygon)
        {
            var p = this._svgToClipper(polygon);
            // remove self-intersections and find the biggest polygon that's left
            var simple = ClipperLib.Clipper.SimplifyPolygon(p, ClipperLib.PolyFillType.pftNonZero);

            if (null == simple || simple.Count == 0)
            {
                return null;
            }

            var biggest = simple[0];
            var biggestarea = Math.Abs(ClipperLib.Clipper.Area(biggest));
            for (var i = 1; i < simple.Count; i++)
            {
                var area = Math.Abs(ClipperLib.Clipper.Area(simple[i]));
                if (area > biggestarea)
                {
                    biggest = simple[i];
                    biggestarea = area;
                }
            }

            // clean up singularities, coincident points and edges
            var clean = ClipperLib.Clipper.CleanPolygon(biggest, _config.curveTolerance * _config.clipperScale);

            if (null == clean || clean.Count == 0)
            {
                return null;
            }

            return this._clipperToSvg(clean);
        }


        private int _toTree(List<Polygon> list, int idstart = 0)
        {
            var parents = new List<Polygon>();
            var i = 0;
            var j = 0;

            // assign a unique id to each leaf
            var id = idstart;

            for (i = 0; i < list.Count; i++)
            {
                var p = list[i];

                var ischild = false;
                for (j = 0; j < list.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (GeometryUtil.PointInPolygon(p[0], list[j]) == true)
                    {
                        if (null != list[j].Children)
                        {
                            list[j].Children = new List<Polygon>();
                        }
                        list[j].Children.Add(p);
                        p.Parent = list[j];
                        ischild = true;
                        break;
                    }
                }

                if (!ischild)
                {
                    parents.Add(p);
                }
            }

            for (i = 0; i < list.Count; i++)
            {
                if (parents.IndexOf(list[i]) < 0)
                {
                    list.splice(i, 1);
                    i--;
                }
            }

            for (i = 0; i < parents.Count; i++)
            {
                parents[i].Id = id;
                id++;
            }

            for (i = 0; i < parents.Count; i++)
            {
                if (null != parents[i].Children)
                {
                    id = this._toTree(parents[i].Children, id);
                }
            }

            return id;
        }

        // assuming no intersections, return a _tree where odd leaves are _parts and even ones are holes
        // might be easier to use the DOM, but paths can't have paths as children. So we'll just make our own _tree.
        private List<Polygon> _getParts(List<Polygon> paths)
        {

            var i = 0;
            //var j = 0;
            var polygons = new List<Polygon>();

            var numChildren = paths.Count;
            for (i = 0; i < numChildren; i++)
            {
                var poly = SvgParser.polygonify(paths[i]);
                poly = this._cleanPolygon(poly);

                // todo: warn user if poly could not be processed and is excluded from the nest
                if (poly != null && poly.Count > 2 && Math.Abs(GeometryUtil.PolygonArea(poly)) > _config.curveTolerance * _config.curveTolerance)
                {
                    poly.Source = i;
                    polygons.Add(poly);
                }
            }

            // turn the list into a _tree
            this._toTree(polygons);

            return polygons;
        }

        public List<Polygon> parsesvg(List<Polygon> svg)
        {
            _svg = svg;
            _tree = _getParts(_svg);
            return _svg;
        }

        public void setbin(Polygon element)
        {
            if (null == _svg)
            {
                return;
            }
            _bin = element;
        }


        // use the clipper library to return an offset to the given polygon. Positive offset expands the polygon, negative contracts
        // note that this returns an array of polygons
        private List<Polygon> _polygonOffset(Polygon polygon, double offset)
        {
            if (offset == 0 || GeometryUtil.AlmostEqual(offset, 0))
            {
                return new List<Polygon> { polygon };
            }

            var p = this._svgToClipper(polygon);

            var miterLimit = 2;
            var co = new ClipperLib.ClipperOffset(miterLimit, _config.curveTolerance * _config.clipperScale);
            co.AddPath(p, ClipperLib.JoinType.jtRound, ClipperLib.EndType.etClosedPolygon);

            var newpaths = new Paths();
            co.Execute(ref newpaths, offset * _config.clipperScale);

            var result = new List<Polygon>();
            for (var i = 0; i < newpaths.Count; i++)
            {
                result.Add(this._clipperToSvg(newpaths[i]));
            }

            return result;
        }


        // offset _tree recursively
        private void _offsetTree(List<Polygon> t, double offset)
        {
            for (var i = 0; i < t.Count; i++)
            {
                var offsetpaths = this._polygonOffset(t[i], offset);

                if (offsetpaths.Count == 1)
                {

                    // replace array items in place
                    //Array.prototype.splice.apply(t[i], [0, t[i].length].concat(offsetpaths[0]));
                    t[i].Points.splice(0, t[i].Count, offsetpaths[0].Points.ToArray());

                }

                if (null != t[i].Children && t[i].Children.Count > 0)
                {
                    this._offsetTree(t[i].Children, -offset);
                }
            }
        }

        // progressCallback is called when _progress is made
        // displayCallback is called when a new placement has been made
        public IEnumerable<List<Polygon>> start(Action<object> progressCallback, Action<object, object, object> displayCallback)
        {
            if (null == _svg || null == _bin)
            {
                return null;
            }
            if (!preparePolygons(progressCallback, displayCallback)) return null;

            this.working = false;

            
            this.working = true;
            _progressCallback(_progress);

            Reverse(_tree);
            _commons.log("Before _launchWorkers ", _tree, _binPolygon);
            return this._launchWorkers(_tree, _binPolygon);
        }

        private bool preparePolygons(Action<object> progressCallback, Action<object, object, object> displayCallback)
        {
            if (null != progressCallback)
            {
                _progressCallback = progressCallback;
            }

            if (null != displayCallback)
            {
                _displayCallback = displayCallback;
            }

            //_parts = Array.prototype.slice.call(_svg.children);
            _parts = new List<Polygon>();
            for (var x = 0; x < _svg.Count; x++)
            {
                _parts.Add(_svg[x]);
            }

            var binindex = _parts.IndexOf(_bin);

            if (binindex >= 0)
            {
                // don't process bin as a part of the _tree
                _parts.splice(binindex, 1);
            }


            // build _tree without bin
            _tree = this._getParts(_parts.slice(0));

            this._offsetTree(_tree, 0.5 * _config.spacing);


            _binPolygon = SvgParser.polygonify(_bin);
            _binPolygon = this._cleanPolygon(_binPolygon);

            if (null == _binPolygon || _binPolygon.Count < 3)
            {
                return false;
            }

            _binBounds = GeometryUtil.GetPolygonBounds(_binPolygon);

            if (_config.spacing > 0)
            {
                var offsetBin = this._polygonOffset(_binPolygon, -0.5 * _config.spacing);
                if (offsetBin.Count == 1)
                {
                    // if the offset contains 0 or more than 1 path, something went wrong.
                    _binPolygon = offsetBin.pop();
                }
            }

            _binPolygon.Id = -1;

            // put bin on origin
            var xbinmax = _binPolygon[0].X;
            var xbinmin = _binPolygon[0].X;
            var ybinmax = _binPolygon[0].Y;
            var ybinmin = _binPolygon[0].Y;

            for (var i = 1; i < _binPolygon.Count; i++)
            {
                if (_binPolygon[i].X > xbinmax)
                {
                    xbinmax = _binPolygon[i].X;
                }
                else if (_binPolygon[i].X < xbinmin)
                {
                    xbinmin = _binPolygon[i].X;
                }

                if (_binPolygon[i].Y > ybinmax)
                {
                    ybinmax = _binPolygon[i].Y;
                }
                else if (_binPolygon[i].Y < ybinmin)
                {
                    ybinmin = _binPolygon[i].Y;
                }
            }

            for (var i = 0; i < _binPolygon.Count; i++)
            {
                _binPolygon[i].X -= xbinmin;
                _binPolygon[i].Y -= ybinmin;
            }

            _binPolygon.Width = xbinmax - xbinmin;
            _binPolygon.Height = ybinmax - ybinmin;

            // all paths need to have the same winding direction
            if (GeometryUtil.PolygonArea(_binPolygon) > 0)
            {
                _binPolygon.Reverse();
            }

            // remove duplicate endpoints, ensure counterclockwise winding direction
            for (var i = 0; i < _tree.Count; i++)
            {
                var start = _tree[i][0];
                var end = _tree[i][_tree[i].Count - 1];
                if (start == end || (GeometryUtil.AlmostEqual(start.X, end.X) && GeometryUtil.AlmostEqual(start.Y, end.Y)))
                {
                    _tree[i].Points.pop();
                }

                if (GeometryUtil.PolygonArea(_tree[i]) > 0)
                {
                    _tree[i].Reverse();
                }
            }

            
            return true;
        }

        private void Reverse(List<Polygon> tree)
        {
            tree.Reverse();
            foreach (var item in tree)
            {
                Reverse(item.Children);
            }

        }

        private List<Polygon> _minkowskiDifference(Polygon A, Polygon B)
        {
            var Ac = _commons.toClipperCoordinates(A);
            Clipper.ScaleUpPath(Ac, 10000000);
            var Bc = _commons.toClipperCoordinates(B);
            Clipper.ScaleUpPath(Bc, 10000000);
            for (var i = 0; i < Bc.Count; i++)
            {
                var t = Bc[i];
                t.X *= -1;
                t.Y *= -1;
                Bc[i] = t;
            }
            var solution = ClipperLib.Clipper.MinkowskiSum(Ac, Bc, true);
            Polygon clipperNfp = null;

            double? largestArea = null;
            for (var i = 0; i < solution.Count; i++)
            {
                var n = _commons.toNestCoordinates(solution[i], 10000000);
                var sarea = GeometryUtil.PolygonArea(n);
                if (largestArea == null || largestArea > sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                }
            }

            for (var i = 0; i < clipperNfp.Count; i++)
            {
                clipperNfp[i].X += B[0].X;
                clipperNfp[i].Y += B[0].Y;
            }

            return new List<Polygon> { clipperNfp };
        }

        private Pair _functionPair(NfpPair pair)
        {
            if (null == pair)
            {
                return null;
            }
            var searchEdges = _config.searchEdges;
            var useHoles = _config.useHoles;

            var A = _commons.rotatePolygon(pair.A, pair.Key.ARotation);
            var B = _commons.rotatePolygon(pair.B, pair.Key.BRotation);

            List<Polygon> nfp;

            if (pair.Key.Inside)
            {
                nfp = generateNfpForInside(pair, A, B, searchEdges);
            }
            else
            {
                nfp = generateStandadNfp(pair, searchEdges, A, B);
                if(nfp==null)return null;

                // generate nfps for children (holes of _parts) if any exist
                if (useHoles && A.Children != null && A.Children.Count > 0)
                {
                    generateNfpWithHoles(B, A, searchEdges, nfp);
                }
            }



            return new Pair
            {
                Key = pair.Key,
                Value = nfp
            };
        }

        private List<Polygon> generateStandadNfp(NfpPair pair, bool searchEdges, Polygon A, Polygon B)
        {
            var nfp = new List<Polygon>();
            if (searchEdges)
            {
                nfp = GeometryUtil.NoFitPolygon(A, B, false, searchEdges);
            }
            else
            {
                nfp = this._minkowskiDifference(A, B);
            }

            // sanity check
            if (null == nfp || nfp.Count == 0)
            {
                _commons.log("NFP Error: ", pair.Key);
                _commons.log("A: ", JsonConvert.SerializeObject(A));
                _commons.log("B: ", JsonConvert.SerializeObject(B));
                return null;
            }

            for (var i = 0; i < nfp.Count; i++)
            {
                if (!searchEdges || i == 0)
                {
                    // if searchedges is active, only the first NFP is guaranteed to pass sanity check
                    if (Math.Abs(GeometryUtil.PolygonArea(nfp[i])) < Math.Abs(GeometryUtil.PolygonArea(A)))
                    {
                        _commons.log("NFP Area Error: ", Math.Abs(GeometryUtil.PolygonArea(nfp[i])), pair.Key);
                        _commons.log("NFP:", JsonConvert.SerializeObject(nfp[i]));
                        _commons.log("A: ", JsonConvert.SerializeObject(A));
                        _commons.log("B: ", JsonConvert.SerializeObject(B));
                        nfp.splice(i, 1);
                        return null;
                    }
                }
            }

            if (nfp.Count == 0)
            {
                return null;
            }

            // for outer NFPs, the first is guaranteed to be the largest. Any subsequent NFPs that lie inside the first are holes
            for (var i = 0; i < nfp.Count; i++)
            {
                if (GeometryUtil.PolygonArea(nfp[i]) > 0)
                {
                    nfp[i].Reverse();
                }

                if (i > 0)
                {
                    if (GeometryUtil.PointInPolygon(nfp[i][0], nfp[0]).Value)
                    {
                        if (GeometryUtil.PolygonArea(nfp[i]) < 0)
                        {
                            nfp[i].Reverse();
                        }
                    }
                }
            }

            return nfp;
        }

        private List<Polygon> generateNfpForInside(NfpPair pair, Polygon A, Polygon B, bool searchEdges)
        {
            List<Polygon> nfp;
            if (GeometryUtil.IsRectangle(A, 0.001))
            {
                nfp = GeometryUtil.NoFitPolygonRectangle(A, B);
            }
            else
            {
                nfp = GeometryUtil.NoFitPolygon(A, B, true, searchEdges);
            }

            // ensure all interior NFPs have the same winding direction
            if (null != nfp && nfp.Count > 0)
            {
                for (var i = 0; i < nfp.Count; i++)
                {
                    if (GeometryUtil.PolygonArea(nfp[i]) > 0)
                    {
                        nfp[i].Reverse();
                    }
                }
            }
            else
            {
                // warning on null inner NFP
                // this is not an error, as the part may simply be larger than the bin or otherwise unplaceable due to geometry
                _commons.log("NFP Warning: ", pair.Key);
            }

            return nfp;
        }

        private static void generateNfpWithHoles(Polygon B, Polygon A, bool searchEdges, List<Polygon> nfp)
        {
            var Bbounds = GeometryUtil.GetPolygonBounds(B);

            for (var i = 0; i < A.Children.Count; i++)
            {
                var Abounds = GeometryUtil.GetPolygonBounds(A.Children[i]);

                // no need to find nfp if B's bounding box is too big
                if (Abounds.Width > Bbounds.Width && Abounds.Height > Bbounds.Height)
                {
                    var cnfp = GeometryUtil.NoFitPolygon(A.Children[i], B, true, searchEdges);
                    // ensure all interior NFPs have the same winding direction
                    if (null != cnfp && cnfp.Count > 0)
                    {
                        for (var j = 0; j < cnfp.Count; j++)
                        {
                            if (GeometryUtil.PolygonArea(cnfp[j]) < 0)
                            {
                                cnfp[j].Reverse();
                            }

                            nfp.Add(cnfp[j]);
                        }
                    }
                }
            }
        }

        private IEnumerable<List<Polygon>> _launchWorkers(List<Polygon> treeLocal, Polygon binPolygonLocal)
        {

            if (_geneticAlgorithm == null)
            {
                // initiate new _geneticAlgorithm
                var adam = treeLocal.slice(0);

                // seed with decreasing area
                adam.Sort((a, b) =>
                {
                    return (int)(Math.Abs(GeometryUtil.PolygonArea(b)) - Math.Abs(GeometryUtil.PolygonArea(a)));
                });

                _geneticAlgorithm = new GeneticAlgorithm();
                _geneticAlgorithm.init(adam, binPolygonLocal, _config);
            }



            // evaluate all members of the population
            for (var i = 0; i < _geneticAlgorithm.population.Count; i++)
            {
                if (null != _geneticAlgorithm.population[i].Fitness)
                {
                    _individual = _geneticAlgorithm.population[i];
                    break;
                }
            }

            if (_individual == null)
            {
                // all individuals have been evaluated, start next generation
                _geneticAlgorithm.generation();
                _individual = _geneticAlgorithm.population[1];
            }

            var placelist = _individual.Placements;
            var rotations = _individual.Rotations;

            var ids = new List<int>();
            for (var i = 0; i < placelist.Count; i++)
            {
                ids.Add(placelist[i].Id);
                placelist[i].Rotation = rotations[i];
            }

            Reverse(placelist);
            _commons.log("Placelist _launchWorkers ",placelist, rotations);
            var nfpPairs = new List<NfpPair>();
            NfpCacheKey key;
            var newCache = new Dictionary<string, List<Polygon>>();

            for (var i = 0; i < placelist.Count; i++)
            {
                var part = placelist[i];
                key = new NfpCacheKey
                {
                    A = binPolygonLocal.Id,
                    B = part.Id,
                    Inside = true,
                    ARotation = 0,
                    BRotation = rotations[i]
                };
                if (!_nfpCache.ContainsKey(JsonConvert.SerializeObject(key)))
                {
                    nfpPairs.Add(new NfpPair
                    {
                        A = binPolygonLocal,
                        B = part,
                        Key = key
                    });
                }
                else
                {
                    newCache[JsonConvert.SerializeObject(key)] = _nfpCache[JsonConvert.SerializeObject(key)];
                }
                for (var j = 0; j < i; j++)
                {
                    var placed = placelist[j];
                    key = new NfpCacheKey
                    {
                        A = placed.Id,
                        B = part.Id,
                        Inside = false,
                        ARotation = rotations[j],
                        BRotation = rotations[i]
                    };
                    if (!_nfpCache.ContainsKey(JsonConvert.SerializeObject(key)))
                    {
                        nfpPairs.Add(new NfpPair
                        {
                            A = placed,
                            B = part,
                            Key = key
                        });
                    }
                    else
                    {
                        newCache[JsonConvert.SerializeObject(key)] = _nfpCache[JsonConvert.SerializeObject(key)];
                    }
                }
            }

            // only keep cache for one cycle
            _nfpCache = newCache;

            return generatePlacements(binPolygonLocal, ids, rotations, nfpPairs, placelist);
        }

        private IEnumerable<List<Polygon>> generatePlacements(Polygon binPolygonLocal, List<int> ids, List<double> rotations, List<NfpPair> nfpPairs, List<Polygon> placelist)
        {
            var worker = new PlacementWorker(binPolygonLocal, ids, rotations, _config);

            var spawncount = 0;

            var generatedNfp = new List<Pair>();
            var generatedPlacements = new List<List<Polygon>>();
            for (var w = 0; w < _config.iterations; w++)
            {
                List<Polygon> res = null;
                try
                {
                    for (var ii = 0; ii < nfpPairs.Count; ii++)
                    {
                        generatedNfp.Add(this._functionPair(nfpPairs[ii]));
                    }

                    res = this._functionGeneratedNfp(generatedNfp, worker, placelist);
                    _progress = spawncount++ / nfpPairs.Count;
                    _commons.log(_progress);
                }
                catch (Exception err)
                {
                    _commons.log(err);
                }

                if (res != null)
                {
                    generatedPlacements.Add(res);
                }
            }

            return generatedPlacements;
        }


        private List<Polygon> _flattenTree(List<Polygon> t, bool hole)
        {
            var flat = new List<Polygon>();
            for (var i = 0; i < t.Count; i++)
            {
                flat.Add(t[i]);
                t[i].Hole = hole;
                if (t[i].Children != null && t[i].Children.Count > 0)
                {
                    flat.AddRange(_flattenTree(t[i].Children, !hole));
                }
            }

            return flat;
        }
        private List<Polygon> _applyPlacement(List<List<Position>> placement)
        {
            //throw new NotImplementedException();
            
            var i = 0;
            var j = 0;
            //var k = 0;
            var clone = new List<Polygon>();
            //create a copy of the list of items
            for (i = 0; i < _parts.Count; i++)
            {
                clone.Add(_parts[i].Clone(false));
            }

            var svglist = new List<Polygon>();

            //foreach blocks of placements (one for each destination bin)
            for (i = 0; i < placement.Count; i++)
            {
                //create a new _svg container
                /*var newsvg = _binPolygon.Clone(true);//_svg.cloneNode(false);
                newsvg.setAttribute("viewBox", "0 0 " + _binBounds.width + " " + _binBounds.height);
                newsvg.setAttribute("width", _binBounds.width + "px");
                newsvg.setAttribute("height", _binBounds.height + "px");*/
                //create a new box to contain the data
                var binclone = _bin.Clone(false);

                //binclone.setAttribute("class", "bin");
                //set it in 0,0 position
                //binclone.setAttribute("transform", "translate(" + (-_binBounds.x) + " " + (-_binBounds.y) + ")");

                //add the box to the list of items
                //newsvg.Add(binclone);
                svglist.Add(binclone);

                //add each polygon to the box
                for (j = 0; j < placement[i].Count; j++)
                {
                    var p = placement[i][j];
                    var part = GeometryUtil.RotatePolygon(_tree[p.Id], p.Rotation,true);
					part = GeometryUtil.OffsetPolygon(part,p.X,p.Y,true);
					
                    // the original path could have transforms and stuff on it, so apply our transforms on a group
                    //var partgroup = document.createElementNS(_svg.namespaceURI, "g");
                    //show it rotated and offseted
                    //partgroup.setAttribute("transform", "translate(" + p.x + " " + p.y + ") rotate(" + p.rotation + ")");
                    //partgroup.appendChild(clone[part.source]);
                    binclone.Children.Add(part);

                    /*if (partOri.Children != null && partOri.Children.Count > 0)
                    {
                        var flattened = this._flattenTree(partOri.children, true);
                        for (k = 0; k < flattened.Count; k++)
                        {

                            var c = clone[flattened[k].source];
                            // add class to indicate hole
                            if (flattened[k].hole && (!c.getAttribute("class") || c.getAttribute("class").indexOf("hole") < 0))
                            {
                                c.setAttribute("class", c.getAttribute("class") + " hole");
                            }
                            partgroup.appendChild(c);
                        }
                    }

                    newsvg.Add(partgroup);*/
                }

                //svglist.Add(newsvg);
            }



            return svglist;
        }
        private List<Polygon> _functionPlacements(List<Placement> placements, List<Polygon> placelist)
        {
            if (null == placements || placements.Count == 0)
            {
                return null;
            }

            _individual.Fitness = placements[0].Fitness;
            var bestresult = placements[0];

            for (var i = 1; i < placements.Count; i++)
            {
                if (placements[i].Fitness < bestresult.Fitness)
                {
                    bestresult = placements[i];
                }
            }

            if (null == _best || bestresult.Fitness < _best.Fitness)
            {
                _best = bestresult;

                double placedArea = 0;
                double totalArea = 0;
                var numParts = placelist.Count;
                var numPlacedParts = 0;

                for (var i = 0; i < _best.Placements.Count; i++)
                {
                    totalArea += Math.Abs(GeometryUtil.PolygonArea(_binPolygon));
                    for (var j = 0; j < _best.Placements[i].Count; j++)
                    {
                        placedArea += Math.Abs(GeometryUtil.PolygonArea(_tree[_best.Placements[i][j].Id]));
                        numPlacedParts++;
                    }
                }
                _displayCallback(null,placedArea / totalArea, numPlacedParts + "/" + numParts);
                this.working = false;
                return this._applyPlacement(_best.Placements);
            }
            else
            {
                _displayCallback(null,null,null);
                return null;
            }
            
        }

        private List<Polygon> _functionGeneratedNfp(List<Pair> generatedNfp, PlacementWorker worker, List<Polygon> placelist)
        {
            if (generatedNfp != null)
            {
                for (var i = 0; i < generatedNfp.Count; i++)
                {
                    var Nfp = generatedNfp[i];

                    if (Nfp != null)
                    {
                        // a null nfp means the nfp could not be generated, either because the _parts simply don't fit or an error in the nfp algo
                        var key = JsonConvert.SerializeObject(Nfp.Key);
                        _nfpCache[key] = Nfp.Value;
                    }
                }
            }
            worker.nfpCache = _nfpCache;


            var placements = new List<Placement>();
            var placelistsliced = new List<List<Polygon>> { placelist.slice(0) };
            try
            {
                for (var i = 0; i < placelistsliced.Count; i++)
                {
                    placements.Add(worker.placePaths(placelistsliced[i]));
                }

                return _functionPlacements(placements, placelist);
            }
            catch (Exception err)
            {
                _commons.log(err);
                return null;
            }

        }
    }
}
