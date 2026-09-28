using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Virinco.WATS.Integration.TextConverter;
using Virinco.WATS.Interface;

namespace Virinco.WATS.Converter.NI.TestStand
{
    public class TeststandHTMLConverter : TextConverterBase
    {
        public TeststandHTMLConverter() : this(new Dictionary<string, string>()) { }
        public TeststandHTMLConverter(IDictionary<string, string> args) : base(args)
        {
            //UUT Serial Number:  093_P0X_568_N01_20131218
            SearchFields.RegExpSearchField regExpSearchField = searchFields.AddRegExpField(UUTField.StationName, ReportReadState.InHeader, @".+Station ID: .+'hdr_value'><b>(?<StationID>[^<]+).*", null, typeof(string));
            regExpSearchField = searchFields.AddRegExpField(UUTField.TestSocketIndex, ReportReadState.InHeader, @".+Test Socket Index: .+'hdr_value'><b>(?<TestSocket>[^<]+).*", null, typeof(short));
            regExpSearchField = searchFields.AddRegExpField(UUTField.SerialNumber, ReportReadState.InHeader, @".+Serial Number: .+'hdr_value'><b>(?<Serialnumber>[^<]+).*", null, typeof(string));
            regExpSearchField = searchFields.AddRegExpField(UUTField.ExecutionTime, ReportReadState.InHeader, @".+Execution Time: .+'hdr_value'><b>(?<ExecTime>[^<]+) seconds.*", null, typeof(double));
            regExpSearchField = searchFields.AddRegExpField("Date", ReportReadState.InHeader, @".+Date: .+'hdr_value'><b>(?<Date>[^<]+).*", null, typeof(string));
            regExpSearchField.AddSubField("Date", typeof(string));
            regExpSearchField = searchFields.AddRegExpField("Time", ReportReadState.InHeader, @".+Time: .+'hdr_value'><b>(?<Time>[^<]+).*", null, typeof(string));
            regExpSearchField.AddSubField("Time", typeof(string));
            regExpSearchField = searchFields.AddRegExpField(UUTField.Operator, ReportReadState.InHeader, @".+Operator: .+'hdr_value'><b>(?<Operator>[^<]+).*", null, typeof(string));
            regExpSearchField = searchFields.AddRegExpField(UUTField.Status, ReportReadState.InHeader, @".+UUT Result: .+'hdr_value'><b>.+(?<Status>Passed|Failed|Terminated|Error).*", null, typeof(UUTStatusType), ReportReadState.InTest);
            regExpSearchField = searchFields.AddRegExpField(UUTField.UseSubFields, ReportReadState.InTest, @"<tr><td class='hdr_name'><b>Project:</b></td><td class='hdr_value'><b>(?<Project>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Process:</b></td><td class='hdr_value'><b>(?<Process>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Operation:</b></td><td class='hdr_value'><b>(?<Operation>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Software.Name:</b></td><td class='hdr_value'><b>(?<SequenceName>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Software.SoftwareVersion:</b></td><td class='hdr_value'><b>(?<SequenceVersion>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Software.TestLimitsVersion:</b></td><td class='hdr_value'><b>(?<TestLimitVersion>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Location.Organisation:</b></td><td class='hdr_value'><b>(?<Organization>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Location.Location:</b></td><td class='hdr_value'><b>(?<Location>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Area:</b></td><td class='hdr_value'><b>Production Test</b></td></tr><tr><td class='hdr_name'><b>Station.Manufacturer:</b></td><td class='hdr_value'><b>(?<Manufacturer>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Station.PartNumber:</b></td><td class='hdr_value'><b>(?<StationPN>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Station.PartVersion:</b></td><td class='hdr_value'><b>(?<StationPNVer>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Station.PartName:</b></td><td class='hdr_value'><b>(?<StationPNName>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>WorkOrderNumber:</b></td><td class='hdr_value'><b>(?<WorkOrderNo>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Unit.Manufacturer:</b></td><td class='hdr_value'><b>(?<UnitManufacturer>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Unit.PartNumber:</b></td><td class='hdr_value'><b>(?<PartNumber>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Unit.PartVersion:</b></td><td class='hdr_value'><b>(?<PartRevision>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>Unit.PartName:</b></td><td class='hdr_value'><b>(?<PartName>[^<]+)</b></td></tr><tr><td class='hdr_name'><b>ExecutionMode:</b></td><td class='hdr_value'><b>(?<ExecMode>[^<]+)", null, typeof(string), ReportReadState.InTest);
            regExpSearchField.AddSubField("Project", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("Process", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("Operation", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("SequenceName", typeof(string), null, UUTField.SequenceName);
            regExpSearchField.AddSubField("SequenceVersion", typeof(string), null, UUTField.SequenceVersion);
            regExpSearchField.AddSubField("TestLimitVersion", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("Organization", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("Location", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("Manufacturer", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("StationPN", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("StationPNVer", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("StationPNName", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("WorkOrderNo", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("UnitManufacturer", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("PartNumber", typeof(string), null, UUTField.PartNumber);
            regExpSearchField.AddSubField("PartRevision", typeof(string), null, UUTField.PartRevisionNumber);
            regExpSearchField.AddSubField("PartName", typeof(string), null, UUTField.MiscUUTInfo);
            regExpSearchField.AddSubField("ExecMode", typeof(string), null, UUTField.MiscUUTInfo);

            regExpSearchField = searchFields.AddRegExpField("SequenceCall", ReportReadState.InTest, @".+Begin Sequence: (?<SequenceName>.*)", null, typeof(string));
            regExpSearchField.AddSubField("SequenceName", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("SequenceFile", ReportReadState.InTest, @"<br>[(](?<SequenceFile>.+)[)]<br>", null, typeof(string));
            regExpSearchField.AddSubField("SequenceFile", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("EndSequence", ReportReadState.InTest, @".*End Sequence: (?<SequenceName>[^<]+)", null, typeof(string));
            regExpSearchField.AddSubField("SequenceName", typeof(string));

            //regExpSearchField = searchFields.AddRegExpField("Done", ReportReadState.InTest, @"<td class='value' .*>Done</td></tr>", null, typeof(string));
            //regExpSearchField = searchFields.AddRegExpField("Passed", ReportReadState.InTest, @"<td class='value' .*>Passed</td></tr>", null, typeof(string));
            //regExpSearchField = searchFields.AddRegExpField("Failed", ReportReadState.InTest, @"<td class='value' .*>Failed</td></tr>", null, typeof(string));
            regExpSearchField = searchFields.AddRegExpField("Step", ReportReadState.InTest, @"<tr><td (class='label' )*colspan='2'.*>(.*;)*(?<StepName>[^<]+?):*(</a>)*</td>.*", null, typeof(string));
            regExpSearchField.AddSubField("StepName", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("Label", ReportReadState.InTest, @"<tr><td class='label'[^>]+>(.*;)*(?<label>[^<]+):\s*</td>", null, typeof(string));
            regExpSearchField.AddSubField("label", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("Value", ReportReadState.InTest, @"<td class='value'[^>]+>(<span[^>]*>)*(?<value>[^<]+)(</span[^>]*>)*</td></tr>", null, typeof(string));
            regExpSearchField.AddSubField("value", typeof(string));

        }

        public void CleanUp()
        {
        }


        string currentStepName = "";
        string currentMultiStepName = "";
        string dateStr = "";
        string lim1, lim2 = "";
        string compOp = "";
        string measure = "";
        string unit = "";
        string status = "";
        string lastLabel = "";
        double stepTime = 0;
        void ResetStep()
        {
            lim1 = lim2 = "";
            compOp = "";
            measure = "";
            unit = "";
            lastLabel = "";
            stepTime = 0;
            status = "";
        }

        protected override bool ProcessMatchedLine(SearchFields.SearchMatch match, ref ReportReadState readState)
        {
            if (match == null)
            {
                //currentUUT.OperationType = apiRef.GetOperationType("1105");
                //Regex regex = new Regex(@".*\x5B(?<SN>\d+)\x5D.*");
                //currentUUT.SerialNumber = regex.Match(apiRef.ConversionSource.SourceFile.Name).Groups["SN"].Value;
                currentUUT.AddMiscUUTInfo("FileName", apiRef.ConversionSource.SourceFile.Name);
                GenerateStep(); //Last step
                apiRef.Submit(SubmitMethod.Online, currentUUT);
                return true;
            }
            if (match.matchField.fieldName == null && match.completeLine.Contains("Project:"))
            {
                SetOperationType((string)match.GetSubField("Operation"));
            }
            else
            {
                switch (match.matchField.fieldName)
                {
                    case "Date":
                        dateStr = (string)match.GetSubField("Date");
                        apiRef.ValidationMode = ValidationModeType.AutoTruncate;
                        break;
                    case "Time":
                        dateStr = dateStr + " " + (string)match.GetSubField("Time");
                        DateTime date;
                        if (!DateTime.TryParseExact(dateStr, "dd MMMM yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                            if (!DateTime.TryParseExact(dateStr, "dddd, MMMM dd, yyyy hh:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                                return false;
                        currentUUT.StartDateTime = date;
                        break;
                    case "SequenceCall":
                        string seqName = (string)match.GetSubField("SequenceName");
                        if (seqName != "MainSequence")
                            currentStep = currentSequence = currentSequence.AddSequenceCall(seqName);
                        break;
                    case "SequenceFile":
                        currentSequence.SequenceName = (string)match.GetSubField("SequenceFile");
                        break;
                    case "EndSequence":
                        if (currentSequence.Parent != null) currentSequence = currentSequence.Parent;
                        break;
                    case "Step":
                        string stepName = (string)match.GetSubField("StepName");
                        if (stepName == "StringLimits" || stepName == "NumericLimits" || stepName == "Limits")
                            break;
                        if (!string.IsNullOrEmpty(currentStepName) && !stepName.StartsWith("Measurement[0]"))
                            GenerateStep();
                        ResetStep();
                        if (stepName == "Measurement")
                            currentMultiStepName = currentStep.Name;
                        else
                            currentStepName = stepName;
                        break;
                    case "Label":
                        lastLabel = (string)match.GetSubField("label");
                        break;
                    case "Value":
                        string value = (string)match.GetSubField("value");
                        switch (lastLabel)
                        {
                            case "Status":
                                status = value;
                                break;
                            case "Module Time":
                                stepTime = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "Comparison Type":
                                compOp = value;
                                break;
                            case "StringExpr":
                            case "Low":
                                lim1 = value;
                                break;
                            case "High":
                                lim2 = value;
                                break;
                            case "Units":
                                unit = value;
                                break;
                            case "Data":
                            case "NumericData":
                            case "StringData":
                                measure = value;
                                break;
                        }
                        break;
                }
            }
            return true;
        }

        private void GenerateStep()
        {
            CompOperatorType compOperatorType = GetCompOperatorType();
            if (measure == "" && compOp == "")
                currentStep = currentSequence.AddGenericStep(currentStepName.StartsWith("If ") ? GenericStepTypes.If : currentStepName.StartsWith("End If") ? GenericStepTypes.End : GenericStepTypes.Action, currentStepName);
            else
            {
                double meas = double.NaN;
                if (measure == "NAN") measure = "NaN";
                if (double.TryParse(measure, NumberStyles.Any, CultureInfo.InvariantCulture, out meas))
                {
                    NumericLimitStep numericLimitStep;
                    if (currentMultiStepName == currentStep.Name && currentStep is NumericLimitStep)
                    {
                        numericLimitStep = (NumericLimitStep)currentStep;
                    }
                    else
                    {
                        if (currentMultiStepName != "")
                            currentStep = numericLimitStep = currentSequence.AddNumericLimitStep(currentMultiStepName);
                        else
                            currentStep = numericLimitStep = currentSequence.AddNumericLimitStep(currentStepName);
                    }

                    if (lim1 != "" && lim2 != "")
                    {
                        if (currentStepName.StartsWith("Measurement["))
                            numericLimitStep.AddMultipleTest(meas, compOperatorType, double.Parse(lim1, CultureInfo.InvariantCulture), double.Parse(lim2, CultureInfo.InvariantCulture), unit, currentStepName);
                        else
                            numericLimitStep.AddTest(meas, compOperatorType, double.Parse(lim1, CultureInfo.InvariantCulture), double.Parse(lim2, CultureInfo.InvariantCulture), unit);
                    }
                    else if (lim1 != "")
                    {
                        if (currentStepName.StartsWith("Measurement["))
                            numericLimitStep.AddMultipleTest(meas, compOperatorType, double.Parse(lim1, CultureInfo.InvariantCulture), unit, currentStepName);
                        else
                            numericLimitStep.AddTest(meas, compOperatorType, double.Parse(lim1, CultureInfo.InvariantCulture), unit);

                    }
                    else
                        if (currentStepName.StartsWith("Measurement["))
                            numericLimitStep.AddMultipleTest(meas, unit, currentStepName);
                        else
                            numericLimitStep.AddTest(meas, unit);
                }
                else
                {
                    StringValueStep stringValueStep;
                    if (currentMultiStepName == currentStep.Name && currentStep is StringValueStep)
                    {
                        stringValueStep = (StringValueStep)currentStep;
                    }
                    else
                    {
                        if (currentMultiStepName != "")
                            currentStep = stringValueStep = currentSequence.AddStringValueStep(currentMultiStepName);
                        else
                            currentStep = stringValueStep = currentSequence.AddStringValueStep(currentStepName);
                    }
                    if (lim1 != "")
                    {
                        if (currentStepName.StartsWith("Measurement["))
                            stringValueStep.AddMultipleTest(compOperatorType, measure, lim1, currentStepName);
                        else
                            stringValueStep.AddTest(compOperatorType, measure, lim1);
                    }
                    else
                        if (currentStepName.StartsWith("Measurement["))
                            stringValueStep.AddMultipleTest(measure, currentStepName);
                        else
                            stringValueStep.AddTest(measure);
                }
                if (!currentStepName.StartsWith("Measurement[")) currentMultiStepName = "";
            }
            StepStatusType stepStatusType;
            if (!EnumTryParse<StepStatusType>(status, out stepStatusType))
                stepStatusType = StepStatusType.Done;
            currentStep.Status = stepStatusType;
            if (!double.IsNaN(stepTime))
                currentStep.StepTime = stepTime;
        }

        CompOperatorType GetCompOperatorType()
        {
            CompOperatorType compOperatorType = CompOperatorType.LOG;
            if (compOp == "")
                return compOperatorType;
            if (compOp.Contains("Case Sensitive"))
                compOperatorType = CompOperatorType.CASESENSIT;
            else if (compOp.Contains("Ignore Case"))
                compOperatorType = CompOperatorType.IGNORECASE;
            else if (compOp.Contains("No Comparison"))
                compOperatorType = CompOperatorType.LOG;
            else
            {
                string optype = new Regex(@"(?<opType>\S+).*").Match(compOp).Groups["opType"].Value;
                EnumTryParse<CompOperatorType>(optype, out compOperatorType);
            }
            return compOperatorType;
        }


        private void SetOperationType(string optypeString)
        {
            try
            {
                string code = null;
                var lower = optypeString.ToLower();
                if (lower.Contains("thermal") || lower.Contains("temperature")) code = "1105";
                else if (lower.Contains("devices")) code = "1106";
                else if (lower.Contains("firmware")) code = "1107";
                else if (lower.Contains("ambient")) code = "1108";
                else if (lower.Contains("heatsink")) code = "1109";
                else if (lower.Contains("interactive")) code = "1110";
                else if (lower.Contains("eti")) code = "1111";
                else if (lower.Contains("ess")) code = "1112";
                else if (lower.Contains("final")) code = "1113";
                else if (lower.Contains("rtm")) code = "1114";
                else if (lower.Contains("acceptance")) code = "1115";
                else if (lower.Contains("cancelled")) code = "1116";

                if (code != null)
                    currentUUT.OperationType = apiRef.GetOperationType(code);
            }
            catch (Exception)
            {
                // Custom operation type codes (1105-1116) may not be configured on this WATS instance.
                // Fall back to default operation type (Final Test = 10) to allow offline testing.
                try { currentUUT.OperationType = apiRef.GetOperationType("10"); } catch { }
            }
        }
    }
}
