using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using Geometry;
using Newtonsoft.Json;

namespace SvgNest
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class PlacementWorker
    {
        public Dictionary<string, List<Polygon>> nfpCache;
        private Polygon _binPolygon;
        private List<int> _ids;
        private List<double> _rotations;
        private SvgNestConfig _config;
        private Commons _commons;

        public PlacementWorker(Polygon binPolygon, List<int> ids, List<double> rotations, SvgNestConfig config)
        {
            this._binPolygon = binPolygon;
            this._ids = ids;
            this._rotations = rotations;
            this._config = config;
            _commons = new Commons();
        }

        public Placement placePaths(List<Polygon> paths)
        {
            if (null == this._binPolygon)
            {
                return null;
            }

            var i = 0;
            var j = 0;
            var k = 0;
            var m = 0;
            var n = 0;
            Polygon path;

            // rotate paths by given rotation
            var rotated = new List<Polygon>();
            for (i = 0; i < paths.Count; i++)
            {
                var r = _commons.rotatePolygon(paths[i], paths[i].Rotation);
                r.Rotation = paths[i].Rotation;
                r.Source = paths[i].Source;
                r.Id = paths[i].Id;
                rotated.Add(r);
            }

            paths = rotated;

            var allplacements = new List<List<Position>>();
            double fitness = 0;
            var binarea = Math.Abs(GeometryUtil.PolygonArea(this._binPolygon));
            string key = null;
            List<Polygon> nfp = null;

            while (paths.Count > 0)
            {
                double? minwidth = null;
                var placed = new List<Polygon>();
                var placements = new List<Position>();
                fitness += 1; // add 1 for each new bin opened (lower fitness is better)

                for (i = 0; i < paths.Count; i++)
                {
                    path = paths[i];

                    // inner NFP
                    key = JsonConvert.SerializeObject(new NfpCacheKey
                    {
                        A = -1,
                        B = path.Id,
                        Inside = true,
                        ARotation = 0,
                        BRotation = path.Rotation
                    });
                    
                    // part unplaceable, skip
                    if (!this.nfpCache.ContainsKey(key))
                    {
                        continue;
                    }
                    var binNfp = this.nfpCache[key];
                    if (binNfp==null || binNfp.Count == 0)
                    {
                        continue;;
                    }

                    // ensure all necessary NFPs exist
                    var error = false;
                    for (j = 0; j < placed.Count; j++)
                    {
                        key = JsonConvert.SerializeObject(new NfpCacheKey
                        {
                            A = placed[j].Id,
                            B = path.Id,
                            Inside = false,
                            ARotation = placed[j].Rotation,
                            BRotation = path.Rotation
                        });
                        
                        if(nfpCache.ContainsKey(key)){
                        	nfp = this.nfpCache[key];
                        }

                        if (null == nfp)
                        {
                            error = true;
                            break;
                        }
                    }

                    // part unplaceable, skip
                    if (error)
                    {
                        continue;
                    }

                    Position position = null;
                    if (placed.Count == 0)
                    {
                        // first placement, put it on the left
                        for (j = 0; j < binNfp.Count; j++)
                        {
                            for (k = 0; k < binNfp[j].Count; k++)
                            {
                                if (position == null || binNfp[j][k].X - path[0].X < position.X)
                                {
                                    position = new Position
                                    {
                                        X = binNfp[j][k].X - path[0].X,
                                        Y = binNfp[j][k].Y - path[0].Y,
                                        Id = path.Id,
                                        Rotation = path.Rotation
                                    };
                                }
                            }
                        }

                        placements.Add(position);
                        placed.Add(path);

                        continue;
                    }

                    var clipperBinNfp = new List<Path>();
                    for (j = 0; j < binNfp.Count; j++)
                    {
                        clipperBinNfp.Add(_commons.toClipperCoordinates(binNfp[j]));
                    }

                    Clipper.ScaleUpPaths(clipperBinNfp, this._config.clipperScale);

                    var clipper = new ClipperLib.Clipper();
                    var combinedNfp = new Paths();


                    for (j = 0; j < placed.Count; j++)
                    {
                        key = JsonConvert.SerializeObject(new NfpCacheKey
                        {
                            A = placed[j].Id,
                            B = path.Id,
                            Inside = false,
                            ARotation = placed[j].Rotation,
                            BRotation = path.Rotation
                        });
                        
                        if(this.nfpCache.ContainsKey(key)){
                        	nfp = this.nfpCache[key];
                       	}

                        if (null == nfp)
                        {
                            continue;
                        }

                        for (k = 0; k < nfp.Count; k++)
                        {
                            var clone = _commons.toClipperCoordinates(nfp[k]);
                            for (m = 0; m < clone.Count; m++)
                            {
                                var t = clone[m];
                                t.X += (long)placements[j].X;
                                t.Y += (long)placements[j].Y;
                                clone[m] = t;
                            }

                            Clipper.ScaleUpPath(clone, this._config.clipperScale);
                            clone = ClipperLib.Clipper.CleanPolygon(clone, 0.0001 * this._config.clipperScale);
                            var areaa = Math.Abs(ClipperLib.Clipper.Area(clone));
                            if (clone.Count > 2 && areaa > 0.1 * this._config.clipperScale * this._config.clipperScale)
                            {
                                clipper.AddPath(clone, ClipperLib.PolyType.ptSubject, true);
                            }
                        }
                    }

                    if (!clipper.Execute(ClipperLib.ClipType.ctUnion, combinedNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    // difference with bin polygon
                    var finalNfp = new Paths();
                    clipper = new ClipperLib.Clipper();

                    clipper.AddPaths(combinedNfp, ClipperLib.PolyType.ptClip, true);
                    clipper.AddPaths(clipperBinNfp, ClipperLib.PolyType.ptSubject, true);
                    //EDR XXX WADDAFUCK
                    if (!clipper.Execute(ClipperLib.ClipType.ctDifference, finalNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    finalNfp = Clipper.CleanPolygons(finalNfp, 0.0001 * this._config.clipperScale);

                    for (j = 0; j < finalNfp.Count; j++)
                    {
                        var areab = Math.Abs(ClipperLib.Clipper.Area(finalNfp[j]));
                        if (finalNfp[j].Count < 3 || areab < 0.1 * this._config.clipperScale * this._config.clipperScale)
                        {
                            finalNfp.splice(j, 1);
                            j--;
                        }
                    }

                    if (null == finalNfp || finalNfp.Count == 0)
                    {
                        continue;
                    }

                    var f = new List<Path>();
                    for (j = 0; j < finalNfp.Count; j++)
                    {
                        // back to normal scale
                        var res = _commons.toNestCoordinates(finalNfp[j], this._config.clipperScale);
                        f.Add(ToIntPointsList(res));
                    }
                    finalNfp = f;

                    // choose placement that results in the smallest bounding box
                    // could use convex hull instead, but it can create oddly shaped nests (triangles or long slivers) which are not optimal for real-world use
                    // todo: generalize gravity direction
                    minwidth = null;
                    double? minarea = null;
                    double? minx = null;
                    Polygon nf;
                    double area = 0;
                    Position shiftvector=null;

                    for (j = 0; j < finalNfp.Count; j++)
                    {
                        nf = ToPolygon(finalNfp[j]);
                        if (Math.Abs(GeometryUtil.PolygonArea(nf)) < 2)
                        {
                            continue;
                        }

                        for (k = 0; k < nf.Count; k++)
                        {
                            var allpoints = new Polygon();
                            for (m = 0; m < placed.Count; m++)
                            {
                                for (n = 0; n < placed[m].Count; n++)
                                {
                                    allpoints.Add(new Point
                                    {
                                        X = placed[m][n].X + placements[m].X,
                                        Y = placed[m][n].Y + placements[m].Y
                                    });
                                }
                            }

                            shiftvector = new Position
                            {
                                X = nf[k].X - path[0].X,
                                Y = nf[k].Y - path[0].Y,
                                Id = path.Id,
                                Rotation = path.Rotation,
                                Nfp = combinedNfp
                            };

                            for (m = 0; m < path.Count; m++)
                            {
                                allpoints.Add(new Point
                                {
                                    X = path[m].X + shiftvector.X,
                                    Y = path[m].Y + shiftvector.Y
                                });
                            }

                            var rectbounds = GeometryUtil.GetPolygonBounds(allpoints);

                            // weigh width more, to help compress in direction of gravity
                            area = rectbounds.Width * 2 + rectbounds.Height;

                            if (minarea == null || area < minarea || (GeometryUtil.AlmostEqual(minarea.Value, area) && (minx == null || shiftvector.X < minx)))
                            {
                                minarea = area;
                                minwidth = rectbounds.Width;
                                position = shiftvector;
                                minx = shiftvector.X;
                            }
                        }
                    }
                    if (position != null)
                    {
                        placed.Add(path);
                        placements.Add(position);
                    }
                }

                if (minwidth != null)
                {
                    fitness += minwidth.Value / binarea;
                }

                for (i = 0; i < placed.Count; i++)
                {
                    var index = paths.IndexOf(placed[i]);
                    if (index >= 0)
                    {
                        paths.splice(index, 1);
                    }
                }

                if (placements != null && placements.Count > 0)
                {
                    allplacements.Add(placements);
                }
                else
                {
                    break; // something went wrong
                }
            }

            // there were parts that couldn't be placed
            fitness += 2 * paths.Count;

            return new Placement
            {
                Placements = allplacements,
                Fitness = fitness,
                Paths = paths,
                Area = binarea
            };
        }

        private Polygon ToPolygon(Path res)
        {
            var result = new Polygon();
            for (var i = 0; i < res.Count; i++)
            {
                result.Add(new Point{X=res[i].X,Y= res[i].Y});
            }
            return result;
        }

        private Path ToIntPointsList(Polygon res)
        {
            var result = new Path();
            for (var i = 0; i < res.Count; i++)
            {
                result.Add(new IntPoint(res[i].X,res[i].Y));
            }
            return result;
        }
    }
}
