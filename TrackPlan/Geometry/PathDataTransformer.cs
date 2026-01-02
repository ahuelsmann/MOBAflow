using System.Globalization;
using System.Text;

namespace Moba.TrackPlan.Geometry
{
    public static class PathDataTransformer
    {
        public static string Transform(string svgPath, double tx, double ty, double rotationDeg)
        {
            if (string.IsNullOrWhiteSpace(svgPath))
                return "M 0 0";

            svgPath = svgPath.Replace(",", " ");

            var tokens = svgPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            int i = 0;
            while (i < tokens.Length)
            {
                string cmd = tokens[i++];

                switch (cmd)
                {
                    case "M":
                        {
                            double x = Parse(tokens[i++]);
                            double y = Parse(tokens[i++]);

                            var p = ApplyTransform(x, y, tx, ty, rotationDeg);

                            sb.Append("M ");
                            sb.Append(p.X.ToString("F1", CultureInfo.InvariantCulture));
                            sb.Append(" ");
                            sb.Append(p.Y.ToString("F1", CultureInfo.InvariantCulture));
                            sb.Append(" ");
                            break;
                        }

                    case "L":
                        {
                            double x = Parse(tokens[i++]);
                            double y = Parse(tokens[i++]);

                            var p = ApplyTransform(x, y, tx, ty, rotationDeg);

                            sb.Append("L ");
                            sb.Append(p.X.ToString("F1", CultureInfo.InvariantCulture));
                            sb.Append(" ");
                            sb.Append(p.Y.ToString("F1", CultureInfo.InvariantCulture));
                            sb.Append(" ");
                            break;
                        }

                    case "A":
                        {
                            double rx = Parse(tokens[i++]);
                            double ry = Parse(tokens[i++]);
                            double svgRotation = Parse(tokens[i++]);
                            int largeArcFlag = (int)Parse(tokens[i++]);
                            int sweepFlag = (int)Parse(tokens[i++]);
                            double endX = Parse(tokens[i++]);
                            double endY = Parse(tokens[i++]);

                            foreach (var p in ApproximateArc(
                                         rx, ry, svgRotation,
                                         largeArcFlag, sweepFlag,
                                         endX, endY,
                                         tx, ty, rotationDeg))
                            {
                                sb.Append("L ");
                                sb.Append(p.X.ToString("F1", CultureInfo.InvariantCulture));
                                sb.Append(" ");
                                sb.Append(p.Y.ToString("F1", CultureInfo.InvariantCulture));
                                sb.Append(" ");
                            }

                            break;
                        }
                }
            }

            return sb.ToString().Trim();
        }

        private static IEnumerable<(double X, double Y)> ApproximateArc(
            double rx, double ry,
            double svgRotation,
            int largeArcFlag,
            int sweepFlag,
            double endX, double endY,
            double tx, double ty,
            double worldRot)
        {
            double totalAngle = (largeArcFlag == 1 ? 330.0 : 30.0);
            if (sweepFlag == 0)
                totalAngle = -totalAngle;

            int steps = 16;
            double stepAngle = totalAngle / steps;

            var points = new List<(double X, double Y)>();

            for (int i = 1; i <= steps; i++)
            {
                double a = (stepAngle * i) * Math.PI / 180.0;

                double x = rx * Math.Sin(a);
                double y = ry * (1.0 - Math.Cos(a));

                points.Add((x, y));
            }

            foreach (var p in points)
            {
                var pt = ApplyTransform(p.X, p.Y, tx, ty, worldRot);
                yield return pt;
            }
        }

        private static (double X, double Y) ApplyTransform(
            double x, double y,
            double tx, double ty,
            double rotDeg)
        {
            double rad = rotDeg * Math.PI / 180.0;

            double xr = x * Math.Cos(rad) - y * Math.Sin(rad);
            double yr = x * Math.Sin(rad) + y * Math.Cos(rad);

            return (xr + tx, yr + ty);
        }

        private static double Parse(string s)
            => double.Parse(s, CultureInfo.InvariantCulture);
    }
}