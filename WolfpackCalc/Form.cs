using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace WolfpackCalc
{
    public partial class Form1 : Form
    {
        private const double KnotsConversionFactor = 1.944;
        private const double MaxSpeedKnots = 44;
        private const double AngleConversionFactor = 16;
        private const double KnotsToMetersPerSecond = 0.514444;
        private static readonly double DegreesToRadians = Math.PI / 180;
        private static readonly double RadiansToDegrees = 180 / Math.PI;
        private static readonly double Epsilon = 1E-10;
        private const int ZoomFactor = 4;
        private bool isAngleMode = true;
        private bool isAttackPeriscope = true;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Wolfpack Calc";
            this.StartPosition = FormStartPosition.CenterScreen;
            var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WolfpackCalc.icon.ico");
            if (iconStream != null)
            {
                this.Icon = new Icon(iconStream);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateMainTabWidths(); // Для основного tabMain
            UpdateBearingTabs(); // Для вложенного tabBearing
            this.Resize += (s, args) => {
                UpdateMainTabWidths();
                UpdateBearingTabs();
            };

            //numericTime.Controls[0].Visible = false; // Отключаем кнопки
            //numericTimeLength.Controls[0].Visible = false; // Отключаем кнопки

            panelSpeed.Location = new Point(
                (tabSpeed.Width - panelSpeed.Width) / 2,
                panelSpeed.Location.Y);
            panelDist.Location = new Point(
                (tabSpeed.Width - panelDist.Width) / 2,
                panelDist.Location.Y);
        }

        private void UpdateMainTabWidths()
        {
            if (tabMain.TabCount == 0) return;

            // Рассчитываем ширину без остатка
            int totalWidth = tabMain.Width - 3; // -2 для коррекции границ
            int tabWidth = totalWidth / tabMain.TabCount;

            // Устанавливаем одинаковый размер для всех вкладок
            tabMain.ItemSize = new Size(tabWidth, tabMain.ItemSize.Height);
        }

        private void UpdateBearingTabs()
        {
            if (tabBearing.TabCount == 0) return;

            // Полная высота минус 2px для границы
            int tabHeight = (tabBearing.Height - 2) / tabBearing.TabCount;
            // Устанавливаем одинаковый размер для всех вкладок
            tabBearing.ItemSize = new Size(tabHeight - 1, tabBearing.ItemSize.Width);
        }

        private void ButtonSpeed_Click(object sender, EventArgs e)
        {
            CalcSpeed();
        }

        private void CalcSpeed()
        {
            double length = (double)numericTimeLength.Value;
            double time = (double)numericTime.Value;

            if (length == 0 || time == 0)
            {
                labelSpeed.Text = "Error: Values must be positive";
                return;
            }

            double speed = Math.Round(length / time * KnotsConversionFactor, 2);

            // Проверяем, что скорость получилась не слишком большая
            if (speed > MaxSpeedKnots)
            {
                labelSpeed.Text = "Error: Speed exceeds maximum";
                labelLastSpeed.Text = "Err";
            }
            else
            {
                labelSpeed.Text = $"Speed: {speed} Kts";
                labelLastSpeed.Text = $"{speed:F2}"; // Всегда 2 знака после запятой
            }
        }

        /// Calculates visible range based on observer height and angle
        public static double CalculateRange(double height, double angle)
        {
            double angleDegrees = angle / AngleConversionFactor;

            // Конвертируем градусы в радианы (Math.Tan использует радианы)
            double angleRadians = angleDegrees * DegreesToRadians;

            // Вычисляем тангенс угла
            double tanValue = Math.Tan(angleRadians);

            // Проверка на деление на ноль и недопустимые углы
            if (Math.Abs(tanValue) < Epsilon) // Практически нулевой тангенс
            {
                throw new ArgumentException("Угол близок к 90 или 270 градусам, тангенс стремится к бесконечности");
            }

            // Вычисляем дистанцию
            double range = height / tanValue;

            // Округляем до 2 знаков (если нужно для вывода)
            return Math.Round(range, 0);
        }

        private void ButtonCalcDist_Click(object sender, EventArgs e)
        {
            CalcDistance();
        }

        private void CalcDistance()
        {
            double height = (double)numericDistHeight.Value;
            double angle = (double)numericDistAngle.Value;

            if (!isAngleMode)
            {
                angle = (double)numericDistAngle.Value * 10;
            }

            if (height == 0 || angle == 0)
            {
                labelDist.Text = "Error: Values must be positive";
                return;
            }

            double dist = CalculateRange(height, angle);
            double dist6 = CalculateRange(ZoomFactor * height, angle);

            // Вычисляем и выводим дистанцию до цели в обоих кратностях перископа
            labelDist.Text = $"Distance 1.5x zoom: {dist:N0} m\nDistance 6x zoom: {dist6:N0} m";
            labeLastDist.Text = (dist > 249999 || dist6 > 99999) ? "too far\ntoo far" : $"{dist:N0}\n{dist6:N0}";
        }

        private void ConfigureNumericForAngles(CustomNumericUpDown numeric)
        {
            numeric.DecimalPlaces = 0;
            numeric.Minimum = 0;
            numeric.Maximum = 360;
            numeric.Increment = 1;
        }

        private void ConfigureNumericForScales(CustomNumericUpDown numeric)
        {
            numeric.DecimalPlaces = 2;
            numeric.Minimum = 0;
            numeric.Maximum = 20;
            numeric.Increment = 0.1m;
        }
        private void CheckBoxDist_CheckedChanged(object sender, EventArgs e)
        {
            ToggleAngleScaleMode();
        }

        private void CheckVisibleHeight_CheckedChanged(object sender, EventArgs e)
        {
            ToggleAngleScaleMode();
        }

        private void ToggleAngleScaleMode()
        {
            // Инвертируем текущий режим
            isAngleMode = !isAngleMode;

            if (!isAngleMode) // SCALE mode
            {
                checkBoxDist.Text = "scale";
                checkVisibleHeight.Text = "scale";
                ConfigureNumericForScales(numericDistAngle);
                ConfigureNumericForScales(numericVPH);
            }
            else // ANGLE mode
            {
                checkBoxDist.Text = " deg.";
                checkVisibleHeight.Text = " deg.";
                ConfigureNumericForAngles(numericDistAngle);
                ConfigureNumericForAngles(numericVPH);
            }

            // Программно выставляем состояние чекбоксов, чтобы они были одинаковы
            checkBoxDist.CheckedChanged -= CheckBoxDist_CheckedChanged;
            checkVisibleHeight.CheckedChanged -= CheckVisibleHeight_CheckedChanged;

            checkBoxDist.Checked = !isAngleMode;
            checkVisibleHeight.Checked = !isAngleMode;

            checkBoxDist.CheckedChanged += CheckBoxDist_CheckedChanged;
            checkVisibleHeight.CheckedChanged += CheckVisibleHeight_CheckedChanged;
        }

        private void ButtonCalcAOB_Click(object sender, EventArgs e)
        {
            CalcAOB();
        }

        private void CalcAOB()
        {
            double targetLength = (double)numericVTL.Value;
            double targetHeight = (double)numericVTH.Value;
            double visualLength = (double)numericVPL.Value;
            double visualHeight = (double)numericVPH.Value;
            double bearingToTarget = (double)numericVBearing.Value;

            Color aobColor = radioRight.Checked ? Color.Green : Color.Red;

            string targetSide = (radioRight.Checked) ? "Right" : "Left";

            // Проверка на нули
            if (targetLength == 0 || targetHeight == 0 || visualLength == 0 || visualHeight == 0)
            {
                labelAOB1.Text = "Error: Invalid value (zero)";
                labelAOB1.ForeColor = Color.Black;
                labelAOB2.Text = "";
                return;
            }

            double aob = CalculateAOB(targetLength, targetHeight, visualLength, visualHeight);

            if (double.IsNaN(aob))
            {
                labelAOB1.Text = "AOB calculation error";
                labelAOB1.ForeColor = Color.Black;
                labelAOB2.Text = "";
                labeVTC1.Text = "";
                labeVTC2.Text = "";
                return;
            }

            bool both = radioBoth.Checked;
            bool retreat = radioRetreat.Checked;

            double aobApproach = aob;
            double aobRetreat = 180 - aob;
            double aobFinal = retreat ? 180 - aob : aobApproach;
            UpdateAOBLabels(aobFinal, targetSide, aobColor, both);

            // Приводим пеленг к абсолютному, добавляя курс подлодки
            if (!isAttackPeriscope)
            {
                bearingToTarget = (bearingToTarget + (double)numericCourse.Value) % 360;
            }

            // Вычисляем курс цели
            if (!both)
            {
                double course = CalculateTargetCourse(bearingToTarget, aobFinal, radioRight.Checked);
                UpdateVTCLabels(course);
            }
            else
            {
                double courseApproach = CalculateTargetCourse(bearingToTarget, aobApproach, radioRight.Checked);
                double courseRetreat = CalculateTargetCourse(bearingToTarget, aobRetreat, radioRight.Checked);
                UpdateVTCLabels(courseApproach, courseRetreat);
            }
        }

        // Вывод результата вычисления AOB
        private void UpdateAOBLabels(double aob, string side, Color color, bool both)
        {
            if (both)
            {
                labelAOB1.Text = $"Approach AOB: {aob:F1}";
                labelAOB1.ForeColor = color;
                labelAOB2.Text = $"Retreat AOB: {(180 - aob):F1}";
                labelAOB2.ForeColor = color;
                panelLAOBB.Visible = true;
                labelLAOB.Text = $"{aob:F1}\n{(180 - aob):F1}";
            }
            else
            {
                labelAOB1.Text = $"AOB {side}: {aob:F1}";
                labelAOB1.ForeColor = color;
                labelAOB2.Text = "";
                panelLAOBB.Visible = false;
                labelLAOBD.Text = side;
                labelLAOB.Text = $"{aob:F1}";
            }
        }

        private void UpdateVTCLabels(double course)
        {
            labeVTC1.Text = $"Target course: {course:F1}";
            labeVTC2.Text = "";
            labelLastCourse.Text = $"{course:F1}";
        }

        private void UpdateVTCLabels(double courseApproach, double courseRetreat)
        {
            labeVTC1.Text = $"Approach course: {courseApproach:F1}";
            labeVTC2.Text = $"Retreat course: {courseRetreat:F1}";
            labelLastCourse.Text = $"App {courseApproach:F1}\nRet {courseRetreat:F1}";
        }

        private double CalculateAOB(double tl, double th, double vl, double vh)
        {
            // Разделим выражение на части для читаемости
            double visualRatio = vl / (vh / AngleConversionFactor);
            double targetRatio = tl / th;
            double ratio = visualRatio / targetRatio;

            double angleRad = Math.Asin(ratio); // здесь можно добавить Clamp при необходимости
            double angleDeg = angleRad * RadiansToDegrees;

            return Math.Round(angleDeg, 1);
        }

        double CalculateTargetCourse(double bearingToTarget, double aob, bool isAobRight)
        {
            double course = isAobRight
                ? (bearingToTarget + (180 - aob)) % 360
                : (bearingToTarget - (180 - aob)) % 360;

            if (course < 0) course += 360; // нормализуем, если ушло в минус
            return course;
        }

        private void CheckPeriscope_CheckedChanged(object sender, EventArgs e)
        {
            PeriscopeMode();
        }

        private void Check3BP_CheckedChanged(object sender, EventArgs e)
        {
            PeriscopeMode();
        }

        private void CheckCP_CheckedChanged(object sender, EventArgs e)
        {
            PeriscopeMode();
        }

        private void PeriscopeMode()
        {
            // Инвертируем текущий режим
            isAttackPeriscope = !isAttackPeriscope;

            if (!isAttackPeriscope) // observation periscope
            {
                checkPeriscope.Text = "  rel.";
                groupSubmarine.Visible = true;

                check3BP.Text = "Observation periscope";
                panel3BC.Visible = true;

                checkCP.Text = "  rel.";
                panelCSC.Visible = true;
            }
            else // attack periscope
            {
                checkPeriscope.Text = "mag.";
                groupSubmarine.Visible = false;

                check3BP.Text = "Attack periscope";
                panel3BC.Visible = false;

                checkCP.Text = "mag.";
                panelCSC.Visible = false;
            }

            // Программно выставляем состояние чекбоксов, чтобы они были одинаковы
            checkPeriscope.CheckedChanged -= CheckPeriscope_CheckedChanged;
            check3BP.CheckedChanged -= Check3BP_CheckedChanged;
            checkCP.CheckedChanged -= CheckCP_CheckedChanged;

            checkPeriscope.Checked = !isAttackPeriscope;
            check3BP.Checked = !isAttackPeriscope;
            checkCP.Checked = !isAttackPeriscope;

            checkPeriscope.CheckedChanged += CheckPeriscope_CheckedChanged;
            check3BP.CheckedChanged += Check3BP_CheckedChanged;
            checkCP.CheckedChanged += CheckCP_CheckedChanged;
        }

        private PointF CalculatePoint(PointF startPoint, double distance, double angleDegrees)
        {
            double angleRad = angleDegrees * DegreesToRadians;

            float x = (float)(startPoint.X + Math.Cos(angleRad) * distance);
            float y = (float)(startPoint.Y + Math.Sin(angleRad) * distance);

            return new PointF(x, y);
        }

        private PointF? FindIntersection(PointF p1, double angle1Deg, PointF p2, double angle2Deg)
        {
            // Переводим углы в радианы
            double angle1Rad = angle1Deg * DegreesToRadians;
            double angle2Rad = angle2Deg * DegreesToRadians;

            // Направляющие векторы
            double dx1 = Math.Cos(angle1Rad);
            double dy1 = Math.Sin(angle1Rad);
            double dx2 = Math.Cos(angle2Rad);
            double dy2 = Math.Sin(angle2Rad);

            // Решаем систему: p1 + t1*(dx1, dy1) = p2 + t2*(dx2, dy2)

            double denominator = dx1 * dy2 - dy1 * dx2;
            if (Math.Abs(denominator) < 1e-6)
            {
                // Прямые параллельны, пересечения нет
                return null;
            }

            double t = ((p2.X - p1.X) * dy2 - (p2.Y - p1.Y) * dx2) / denominator;

            float ix = (float)(p1.X + t * dx1);
            float iy = (float)(p1.Y + t * dy1);

            return new PointF(ix, iy);
        }

        private double CalculateAngleBetweenPoints(PointF p1, PointF p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double angleRad = Math.Atan2(dy, dx); // угол в радианах
            double angleDeg = angleRad * RadiansToDegrees;

            // Переводим в диапазон 0..360
            if (angleDeg < 0)
                angleDeg += 360;

            return angleDeg;
        }

        private bool AreBearingsMonotonic(double b1, double b2, double b3)
        {
            // Нормализует разницу to–from в диапазон (-180, +180]
            double NormalizeDelta(double from, double to)
            {
                double diff = to - from;
                diff = (diff + 540) % 360 - 180;
                return diff;
            }

            double d12 = NormalizeDelta(b1, b2);
            double d23 = NormalizeDelta(b2, b3);

            // строго в одну сторону (никаких нулей и смены знака)
            return (d12 > 0 && d23 > 0)   // всё по часовой
                || (d12 < 0 && d23 < 0);  // всё против часовой
        }

        private void Button3BCourse_Click(object sender, EventArgs e)
        {
            Calc3BCourse();
        }

        private void Calc3BCourse()
        {
            double bearing1 = (double)numericBearing1.Value;
            double bearing2 = (double)numericBearing2.Value;
            double bearing3 = (double)numericBearing3.Value;

            if (!AreBearingsMonotonic(bearing1, bearing2, bearing3))
            {
                label3BTC.Text = "The sequence is broken";
                return;
            }

            if (!isAttackPeriscope)
            {
                double subCourse = (double)numeric3BSC.Value;
                bearing1 = (bearing1 + subCourse) % 360;
                bearing2 = (bearing2 + subCourse) % 360;
                bearing3 = (bearing3 + subCourse) % 360;
            }

            double bearing1Abstract = (bearing1 + 180) % 360;
            double bearing3Abstract = (bearing3 + 180) % 360;

            double distanceToVirtualObserver = 10000;

            PointF point0 = new PointF(0f, 0f);
            PointF position1 = new PointF(0f, 0f);
            PointF position2 = new PointF(0f, 0f);

            // Позиция абстрактного наблюдателя
            PointF pointAbstract = CalculatePoint(point0, distanceToVirtualObserver, bearing2);

            // Предполагаемая начальная позиция цели
            PointF? intersection = FindIntersection(point0, bearing1, pointAbstract, bearing3Abstract);
            if (intersection.HasValue)
            {
                position1 = intersection.Value;
            }
            else
            {
                label3BTC.Text = "Calculations are impossible";
                return;
            }

            // Предполагаемая конечная позиция цели
            intersection = FindIntersection(point0, bearing3, pointAbstract, bearing1Abstract);
            if (intersection.HasValue)
            {
                position2 = intersection.Value;
            }
            else
            {
                label3BTC.Text = "Input data error";
                return;
            }

            double angleBetweenPoints = CalculateAngleBetweenPoints(position1, position2);
            label3BTC.Text = $"Target Course: {angleBetweenPoints:F2}°";
            labelLastCourse.Text = $"{angleBetweenPoints:F2}";
            numericCTC.Value = (decimal)Math.Round(angleBetweenPoints, 2);
        }

        private void ButtonConvert_Click(object sender, EventArgs e)
        {
            ConvertCourseToAOB();
        }

        private void ConvertCourseToAOB()
        {
            double targetCourse = (double)numericCTC.Value;
            double bearing = (double)numericCB.Value;
            Color colorSide;

            if (!isAttackPeriscope)
            {
                bearing = (bearing + (double)numericCSC.Value) % 360;
            }

            (int aob, string side) = CalculateAOB(targetCourse, bearing);

            switch (side)
            {
                case "Right":
                    colorSide = Color.Green;
                    break;
                case "Left":
                    colorSide = Color.Red;
                    break;
                case "Dead ahead":
                    colorSide = Color.Blue;
                    break;
                case "Dead astern":
                    colorSide = Color.Orange;
                    break;
                default:
                    colorSide = Color.Black; // на всякий случай
                    break;
            }

            labelCAOB.Text = $"AOB: {side} {aob:F0}";
            labelCAOB.ForeColor = colorSide;
            panelLAOBB.Visible = false;
            labelLAOBD.Text = side;
            labelLAOB.Text = $"{aob:F1}";
        }

        public static (int angle, string side) CalculateAOB(double targetCourse, double bearing)
        {
            // Рассчитываем обратный пеленг (от цели к наблюдателю)
            double relativeBearing = (bearing + 180) % 360;

            // Вычисляем AOB
            double rawAOB = (relativeBearing - targetCourse + 360) % 360;

            int aob;
            string side;

            if (rawAOB == 0)
            {
                aob = 0;
                side = "Ahead"; // наблюдатель точно по носу цели
            }
            else if (rawAOB == 180)
            {
                aob = 180;
                side = "Astern"; // наблюдатель строго в корме
            }
            else if (rawAOB < 180)
            {
                aob = (int)Math.Round(rawAOB);
                side = "Right"; // правый борт
            }
            else
            {
                aob = (int)Math.Round(360 - rawAOB);
                side = "Left"; // левый борт
            }

            return (aob, side);
        }

        private void ButtonSPCalc_Click(object sender, EventArgs e)
        {
            OutputPreemptive();
        }

        private void OutputPreemptive()
        {
            double distanceSH = (double)numericSHD.Value;
            double timeSH = (double)numericSHT.Value;
            
            if (distanceSH == 0 || timeSH == 0)
            {
                label30knots.Text = label40knots.Text = label44knots.Text = "Invalid value";
                return;
            }

            (double? leadAngle30, double? timeToHit30) = CalculateTorpedoLead(distanceSH, 30, timeSH);
            (double? leadAngle40, double? timeToHit40) = CalculateTorpedoLead(distanceSH, 40, timeSH);
            (double? leadAngle44, double? timeToHit44) = CalculateTorpedoLead(distanceSH, 44, timeSH);

            /*
            if (leadAngle30.HasValue)
            {
                label30knots.Text = $"{leadAngle30.Value:F2} °";
            }
            else
            {
                label30knots.Text = "Torpedo too slow";
            }

            if (leadAngle30.HasValue)
            {
                label40knots.Text = $"{leadAngle40.Value:F2} °";
            }
            else
            {
                label40knots.Text = "Torpedo too slow";
            }

            if (leadAngle30.HasValue)
            {
                label44knots.Text = $"{leadAngle44.Value:F2} °";
            }
            else
            {
                label44knots.Text = "Torpedo too slow";
            } */

            if (leadAngle30.HasValue && timeToHit30.HasValue)
            {
                label30knots.Text = $"{leadAngle30.Value:F2} °\n{timeToHit30.Value / 60:F0}m {timeToHit30.Value % 60:F0}s";
            }
            else
            {
                label30knots.Text = "Torpedo too slow";
            }

            if (leadAngle40.HasValue && timeToHit40.HasValue)
            {
                label40knots.Text = $"{leadAngle40.Value:F2} °\n{timeToHit40.Value / 60:F0}m {timeToHit40.Value % 60:F0}s";
            }
            else
            {
                label40knots.Text = "Torpedo too slow";
            }

            if (leadAngle44.HasValue && timeToHit44.HasValue)
            {
                label44knots.Text = $"{leadAngle44.Value:F2} °\n{timeToHit44.Value / 60:F0}m {timeToHit44.Value % 60:F0}s";
            }
            else
            {
                label44knots.Text = "Torpedo too slow";
            }
            
        }

        private (double?, double?) CalculateTorpedoLead(double distanceToTarget, double torpedoSpeedKnots, double timePerDegree)
        {
            // Конвертируем скорость торпеды из узлов в м/с
            double torpedoSpeedMs = torpedoSpeedKnots * KnotsToMetersPerSecond;
            double timeToTarget = distanceToTarget / torpedoSpeedMs;
            double lead = timeToTarget / timePerDegree;
            return (lead, timeToTarget);

            /*
            // Рассчитываем угловую скорость цели (рад/сек)
            double angularSpeedDegPerSec = 1 / timePerDegree;
            double angularSpeedRadPerSec = angularSpeedDegPerSec * DegreesToRadians;

            // Линейная скорость цели (м/с)
            double targetSpeedMs = distanceToTarget * angularSpeedRadPerSec;

            // Проверка возможности перехвата
            if (torpedoSpeedMs <= targetSpeedMs)
                return null;

            // Рассчитываем угол упреждения (радианы)
            double leadAngleRad = distanceToTarget * angularSpeedRadPerSec /
                                Math.Sqrt(Math.Pow(torpedoSpeedMs, 2) - Math.Pow(targetSpeedMs, 2));

            // Конвертируем в градусы и возвращаем
            return leadAngleRad * RadiansToDegrees; */
        }

        private void numericSHT_Validated(object sender, EventArgs e)
        {
            OutputPreemptive();
        }

        private void numericSHD_Validated(object sender, EventArgs e)
        {
            OutputPreemptive();
        }

        private void numericSHT_ValueChanged(object sender, EventArgs e)
        {
            OutputPreemptive();
        }

        private void numericSHD_ValueChanged(object sender, EventArgs e)
        {
            OutputPreemptive();
        }

        private void numericTimeLength_Validated(object sender, EventArgs e)
        {
            CalcSpeed();
        }

        private void numericTimeLength_ValueChanged(object sender, EventArgs e)
        {
            CalcSpeed();
        }

        private void numericTime_Validated(object sender, EventArgs e)
        {
            CalcSpeed();
        }

        private void numericTime_ValueChanged(object sender, EventArgs e)
        {
            CalcSpeed();
        }

        private void numericDistHeight_Validated(object sender, EventArgs e)
        {
            CalcDistance();
        }

        private void numericDistHeight_ValueChanged(object sender, EventArgs e)
        {
            CalcDistance();
        }

        private void numericDistAngle_ValueChanged(object sender, EventArgs e)
        {
            CalcDistance();
        }

        private void numericDistAngle_Validated(object sender, EventArgs e)
        {
            CalcDistance();
        }
    }
}
