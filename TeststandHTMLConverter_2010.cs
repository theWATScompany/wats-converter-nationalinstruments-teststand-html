using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Virinco.WATS.Integration.TextConverter;
using Virinco.WATS.Interface;

namespace Virinco.WATS.Converter.NI.TestStand
{
    // Converter for the older TestStand HTML 3.2 report format (circa 2010 and earlier).
    //
    // Key structural differences from TeststandHTMLConverter (newer format):
    //   - HTML 3.2: all-caps tags, BGCOLOR= attributes instead of CSS classes
    //   - Header rows use <TD NOWRAP='NOWRAP'><B><LI>Field: </B><TD><B>Value</B>
    //   - UUT Result is wrapped in <FONT COLOR=#...> rather than <span style=...>
    //   - Date is in Norwegian locale ("30. april 2026"); Time is separate
    //   - Each step's label and value appear on the SAME <TR> line
    //   - An explicit "SequenceCall_Name" step precedes every Begin Sequence block
    //   - No custom UDF block (no Project / Process / Operation / SequenceName fields)
    public class TeststandHTMLConverter_2010 : TextConverterBase
    {
        public TeststandHTMLConverter_2010() : this(new Dictionary<string, string>()) { }

        public TeststandHTMLConverter_2010(IDictionary<string, string> args) : base(args)
        {
            // â”€â”€ Header fields â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            searchFields.AddRegExpField(UUTField.StationName, ReportReadState.InHeader,
                @"<TR><TD NOWRAP='NOWRAP'><B><LI>Station ID: </B><TD><B>(?<StationID>[^<]+)</B>",
                null, typeof(string));

            searchFields.AddRegExpField(UUTField.SerialNumber, ReportReadState.InHeader,
                @"<TR><TD NOWRAP='NOWRAP'><B><LI>Serial Number: </B><TD><B>(?<Serialnumber>[^<]+)</B>",
                null, typeof(string));

            searchFields.AddRegExpField(UUTField.Operator, ReportReadState.InHeader,
                @"<TR><TD NOWRAP='NOWRAP'><B><LI>Operator: </B><TD><B>(?<Operator>[^<]+)</B>",
                null, typeof(string));

            // Date and Time are combined in ProcessMatchedLine before parsing
            SearchFields.RegExpSearchField regExpSearchField;

            regExpSearchField = searchFields.AddRegExpField("Date", ReportReadState.InHeader,
                @"<TR><TD NOWRAP='NOWRAP'><B><LI>Date: </B><TD><B>(?<Date>[^<]+)</B>",
                null, typeof(string));
            regExpSearchField.AddSubField("Date", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("Time", ReportReadState.InHeader,
                @"<TR><TD NOWRAP='NOWRAP'><B><LI>Time: </B><TD><B>(?<Time>[^<]+)</B>",
                null, typeof(string));
            regExpSearchField.AddSubField("Time", typeof(string));

            // UUT Result â€” transitions read state to InTest.
            // Status text sits inside a <FONT COLOR=...> element.
            searchFields.AddRegExpField(UUTField.Status, ReportReadState.InHeader,
                @"<TR><TD NOWRAP='NOWRAP'><B><LI>UUT Result: </B><TD><B><FONT COLOR=[^>]+>(?<Status>Passed|Failed|Terminated|Error)</FONT></B>",
                null, typeof(UUTStatusType), ReportReadState.InTest);

            // â”€â”€ Sequence structure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // The Begin/End Sequence markup is the same H5/NOBR structure as newer
            // TestStand versions; only the surrounding tags differ.
            regExpSearchField = searchFields.AddRegExpField("SequenceCall", ReportReadState.InTest,
                @"<H5><NOBR>Begin Sequence: (?<SequenceName>.*)",
                null, typeof(string));
            regExpSearchField.AddSubField("SequenceName", typeof(string));

            // Sequence file path appears on the line following Begin Sequence:
            // <BR>(C:\path\to\file.seq)<BR></NOBR></H5>
            regExpSearchField = searchFields.AddRegExpField("SequenceFile", ReportReadState.InTest,
                @"<BR>[(](?<SequenceFile>.+)[)]<BR></NOBR></H5>",
                null, typeof(string));
            regExpSearchField.AddSubField("SequenceFile", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("EndSequence", ReportReadState.InTest,
                @"<H5><NOBR>End Sequence: (?<SequenceName>[^<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("SequenceName", typeof(string));

            // â”€â”€ Step header row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // BGCOLOR identifies the step type:
            //   #ff8000 = orange  â†’ Action / generic step
            //   #00c4c4 = cyan    â†’ Measured / numeric-limit step
            //   #add8e6 = blue    â†’ loop-body variant (same structure as above)
            // The step name is everything after the BGCOLOR attribute on that row.
            // Rows with BGCOLOR=#ffffff are label+value data rows, not step headers.
            regExpSearchField = searchFields.AddRegExpField("Step", ReportReadState.InTest,
                @"<TR><TD COLSPAN=2 BGCOLOR=#(?:ff8000|00c4c4|add8e6)>(?<StepName>[^<\r\n]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("StepName", typeof(string));

            // â”€â”€ Label+value rows (all on one <TR> line in this older format) â”€â”€â”€â”€â”€â”€â”€â”€â”€

            // Status value â€” BGCOLOR of the value cell encodes Passed/Failed/Done/etc.
            regExpSearchField = searchFields.AddRegExpField("StepStatus", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>Status:\s+<TD BGCOLOR=#[0-9a-fA-F]+ WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<Status>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("Status", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("Measurement", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>Measurement:<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<Measurement>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("Measurement", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("Low", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>&nbsp;&nbsp;&nbsp;Low:<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<Low>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("Low", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("High", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>&nbsp;&nbsp;&nbsp;High:<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<High>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("High", typeof(string));

            regExpSearchField = searchFields.AddRegExpField("CompType", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>Comparison Type:<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<CompType>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("CompType", typeof(string));

            // Units â€” not present in the sample files but expected in some sequences
            regExpSearchField = searchFields.AddRegExpField("Units", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>Units:<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<Units>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("Units", typeof(string));

            // String-limit steps â€” predicted from older TestStand HTML report structure.
            // StringExpr holds the expected string limit; StringData holds the measured value.
            regExpSearchField = searchFields.AddRegExpField("StringExpr", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>StringExpr:<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<StringExpr>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("StringExpr", typeof(string));

            // "String", "StringData", and "String Data" are all plausible label spellings
            // in older TestStand versions for the measured string value.
            regExpSearchField = searchFields.AddRegExpField("StringData", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>(?:String Data|StringData|String):<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<StringData>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("StringData", typeof(string));

            // Alternative label for numeric measurements seen in some older sequences
            regExpSearchField = searchFields.AddRegExpField("NumericData", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>(?:Numeric Data|NumericData):<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<NumericData>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("NumericData", typeof(string));

            // Per-step execution time â€” not observed in samples but present in some
            // older TestStand report configurations.
            regExpSearchField = searchFields.AddRegExpField("ModuleTime", ReportReadState.InTest,
                @"<TR><TD BGCOLOR=#ffffff valign=""top""><FONT SIZE=-1>Module Time:\s+<TD BGCOLOR=#32ffff WIDTH = 37% valign=""top""><FONT SIZE=-1>(?<ModuleTime>[^\r\n<]+)",
                null, typeof(string));
            regExpSearchField.AddSubField("ModuleTime", typeof(string));
        }

        // â”€â”€ Converter parameters â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Regex pattern applied to the source filename to extract the part number.
        /// The first capture group is used as the part number.
        /// Default extracts the first token before '-' (e.g., "ACB" from "ACB-TestSequence...").
        /// </summary>
        private string PartNumberPattern => GetParameterString("PartNumberPattern", @"^(?<PartNumber>[^-]+)");

        /// <summary>
        /// Process code to assign to all converted reports. TestStand reports do not
        /// contain process information, so this must be set externally.
        /// </summary>
        private string ProcessCode => GetParameterString("ProcessCode", "10");

        private string GetParameterString(string key, string defaultValue)
        {
            if (ConverterParameters != null && ConverterParameters.ContainsKey(key))
                return ConverterParameters[key];
            return defaultValue;
        }

        // â”€â”€ Per-step state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        string currentStepName = "";
        string dateStr = "";
        string lim1 = "", lim2 = "";
        string compOp = "";
        string measure = "";
        string unit = "";
        string status = "";
        double stepTime = 0;

        void ResetStep()
        {
            lim1 = lim2 = "";
            compOp = "";
            measure = "";
            unit = "";
            stepTime = 0;
            status = "";
        }

        // â”€â”€ Core processing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        protected override bool ProcessMatchedLine(SearchFields.SearchMatch match, ref ReportReadState readState)
        {
            // match == null signals end-of-file
            if (match == null)
            {
                string fileName = apiRef.ConversionSource.SourceFile.Name;
                currentUUT.AddMiscUUTInfo("FileName", fileName);

                // Serial number in report is "ProductionSite#Serial" (e.g., "959836#26060036").
                // Strip the production site prefix; keep only the part after '#'.
                // Retest suffix (e.g., "-00001") is preserved as part of the serial.
                string sn = currentUUT.SerialNumber;
                if (!string.IsNullOrEmpty(sn) && sn.Contains("#"))
                {
                    currentUUT.AddMiscUUTInfo("ProductionSite", sn.Substring(0, sn.IndexOf('#')));
                    currentUUT.SerialNumber = sn.Substring(sn.IndexOf('#') + 1);
                }

                // Extract part number from filename using configurable regex pattern.
                var partMatch = Regex.Match(fileName, PartNumberPattern);
                if (partMatch.Success)
                    currentUUT.PartNumber = partMatch.Groups["PartNumber"].Success
                        ? partMatch.Groups["PartNumber"].Value
                        : partMatch.Groups[1].Value;

                // Set process from converter parameter (TestStand reports have no process field).
                currentUUT.OperationType = apiRef.GetOperationType(ProcessCode);

                GenerateStep(); // flush last accumulated step
                apiRef.Submit(SubmitMethod.Online, currentUUT);
                return true;
            }

            switch (match.matchField.fieldName)
            {
                case "Date":
                    dateStr = (string)match.GetSubField("Date");
                    apiRef.ValidationMode = ValidationModeType.AutoTruncate;
                    break;

                case "Time":
                    // Combine date + time then parse. The date is in Norwegian locale
                    // ("30. april 2026"); fall back to English formats used by other
                    // TestStand versions if the primary parse fails.
                    dateStr = dateStr + " " + (string)match.GetSubField("Time");
                    DateTime date;
                    if (!DateTime.TryParseExact(dateStr, "d. MMMM yyyy H:mm:ss",
                            CultureInfo.GetCultureInfo("nb-NO"), DateTimeStyles.None, out date))
                        if (!DateTime.TryParseExact(dateStr, "dd MMMM yyyy HH:mm:ss",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                            if (!DateTime.TryParseExact(dateStr, "dddd, MMMM dd, yyyy hh:mm:ss tt",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                                return false;
                    currentUUT.StartDateTime = date;
                    break;

                case "SequenceCall":
                    string seqName = (string)match.GetSubField("SequenceName");
                    if (seqName != "MainSequence")
                    {
                        // Flush whatever step was accumulating before entering this subsequence
                        GenerateStep();
                        ResetStep();
                        currentStepName = "";
                        currentStep = currentSequence = currentSequence.AddSequenceCall(seqName);
                    }
                    break;

                case "SequenceFile":
                    currentSequence.SequenceName = (string)match.GetSubField("SequenceFile");
                    break;

                case "EndSequence":
                    if (currentSequence.Parent != null)
                        currentSequence = currentSequence.Parent;
                    break;

                case "Step":
                    string stepName = (string)match.GetSubField("StepName");

                    // "SequenceCall_Name" rows are just parent-level summary entries
                    // for the subsequence that follows immediately after them.
                    // The real subsequence is opened by the matching "Begin Sequence"
                    // line, so we discard these wrapper steps entirely.
                    if (stepName.StartsWith("SequenceCall_"))
                    {
                        GenerateStep();
                        ResetStep();
                        currentStepName = "";
                        break;
                    }

                    if (!string.IsNullOrEmpty(currentStepName))
                        GenerateStep();
                    ResetStep();
                    currentStepName = stepName;
                    break;

                case "StepStatus":
                    status = ((string)match.GetSubField("Status")).Trim();
                    break;

                case "Measurement":
                    measure = ((string)match.GetSubField("Measurement")).Trim();
                    break;

                case "Low":
                    lim1 = ((string)match.GetSubField("Low")).Trim();
                    break;

                case "High":
                    lim2 = ((string)match.GetSubField("High")).Trim();
                    break;

                case "CompType":
                    compOp = ((string)match.GetSubField("CompType")).Trim();
                    break;

                case "Units":
                    unit = ((string)match.GetSubField("Units")).Trim();
                    break;

                case "StringExpr":
                    // StringExpr is the expected/limit string value
                    lim1 = ((string)match.GetSubField("StringExpr")).Trim();
                    break;

                case "StringData":
                    measure = ((string)match.GetSubField("StringData")).Trim();
                    break;

                case "NumericData":
                    measure = ((string)match.GetSubField("NumericData")).Trim();
                    break;

                case "ModuleTime":
                    double.TryParse((string)match.GetSubField("ModuleTime"),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out stepTime);
                    break;
            }

            return true;
        }

        // â”€â”€ Step generation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void GenerateStep()
        {
            if (string.IsNullOrEmpty(currentStepName))
                return;

            CompOperatorType compOperatorType = GetCompOperatorType();

            if (string.IsNullOrEmpty(measure) && string.IsNullOrEmpty(compOp))
            {
                // No measurement data â†’ generic step (Action, If, End If)
                currentStep = currentSequence.AddGenericStep(
                    currentStepName.StartsWith("If ") ? GenericStepTypes.If :
                    currentStepName.StartsWith("End If") ? GenericStepTypes.End :
                    GenericStepTypes.Action,
                    currentStepName);
            }
            else
            {
                double meas = double.NaN;
                if (measure == "NAN") measure = "NaN";

                if (double.TryParse(measure, NumberStyles.Any, CultureInfo.InvariantCulture, out meas))
                {
                    NumericLimitStep numericLimitStep = currentSequence.AddNumericLimitStep(currentStepName);
                    currentStep = numericLimitStep;

                    if (!string.IsNullOrEmpty(lim1) && !string.IsNullOrEmpty(lim2))
                        numericLimitStep.AddTest(meas, compOperatorType,
                            double.Parse(lim1, CultureInfo.InvariantCulture),
                            double.Parse(lim2, CultureInfo.InvariantCulture), unit);
                    else if (!string.IsNullOrEmpty(lim1))
                        numericLimitStep.AddTest(meas, compOperatorType,
                            double.Parse(lim1, CultureInfo.InvariantCulture), unit);
                    else
                        numericLimitStep.AddTest(meas, unit);
                }
                else
                {
                    StringValueStep stringValueStep = currentSequence.AddStringValueStep(currentStepName);
                    currentStep = stringValueStep;

                    if (!string.IsNullOrEmpty(lim1))
                        stringValueStep.AddTest(compOperatorType, measure, lim1);
                    else
                        stringValueStep.AddTest(measure);
                }
            }

            StepStatusType stepStatusType;
            if (!EnumTryParse<StepStatusType>(status, out stepStatusType))
                stepStatusType = StepStatusType.Done;
            currentStep.Status = stepStatusType;

            if (stepTime > 0)
                currentStep.StepTime = stepTime;
        }

        private CompOperatorType GetCompOperatorType()
        {
            CompOperatorType compOperatorType = CompOperatorType.LOG;
            if (string.IsNullOrEmpty(compOp))
                return compOperatorType;

            if (compOp.Contains("Case Sensitive"))
                compOperatorType = CompOperatorType.CASESENSIT;
            else if (compOp.Contains("Ignore Case"))
                compOperatorType = CompOperatorType.IGNORECASE;
            else if (compOp.Contains("No Comparison"))
                compOperatorType = CompOperatorType.LOG;
            else
            {
                // Comparison types are like "GELE (>= <=)" â€” take the first token
                string optype = new Regex(@"(?<opType>\S+).*").Match(compOp).Groups["opType"].Value;
                EnumTryParse<CompOperatorType>(optype, out compOperatorType);
            }

            return compOperatorType;
        }
    }
}
