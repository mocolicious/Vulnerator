using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Vulnerator.Helper;
using Vulnerator.Model.DataAccess;
using Vulnerator.Model.Object;
using Vulnerator.ViewModel;
using File = Vulnerator.Model.Object.File;

namespace Vulnerator.Model.BusinessLogic.Reports
{
    public class OpenXmlStigDiscrepanciesReportCreator
    {
        private Dictionary<string, int> sharedStringDictionary = new Dictionary<string, int>();
        private int sharedStringMaxIndex;
        private UInt32Value sheetIndex = 1;
        private DdlReader _ddlReader = new DdlReader();
        private Assembly assembly = Assembly.GetExecutingAssembly();
        private string _storedProcedureBase = "Vulnerator.Resources.DdlFiles.StoredProcedures.";
        private OpenXmlWriter _openXmlWriter;
        private readonly OpenXmlStylesheetCreator _openXmlStylesheetCreator = new OpenXmlStylesheetCreator();
        private readonly OpenXmlCellDataHandler _openXmlCellDataHandler = new OpenXmlCellDataHandler();

        public string CreateDiscrepanciesReport(string fileName)
        {
            try
            {
                if (!DatabaseBuilder.sqliteConnection.State.ToString().Equals("Open"))
                {
                    DatabaseBuilder.sqliteConnection.Open();
                }

                using (SpreadsheetDocument spreadsheetDocument =
                    SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook))
                {
                    LogWriter.LogStatusUpdate("Creating STIG Discrepancies workbook framework.");
                    WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                    WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                    workbookStylesPart.Stylesheet = _openXmlStylesheetCreator.CreateStylesheet();
                    Workbook workbook = workbookPart.Workbook = new Workbook();
                    Sheets sheets = workbook.AppendChild(new Sheets());
                    StartReport(workbookPart, sheets);
                    LogWriter.LogStatusUpdate("Writing STIG Discrepancies findings.");
                    WriteFindingsToReport();
                    LogWriter.LogStatusUpdate("Finalizing STIG Discrepancies workbook.");
                    EndReport();
                    _openXmlCellDataHandler.CreateSharedStringPart(workbookPart, sharedStringMaxIndex,
                        sharedStringDictionary);
                }

                return "Excel report creation successful";
            }
            catch (Exception exception)
            {
                string error = $"Unable to create '{fileName}' (Excel Report).";
                LogWriter.LogErrorWithDebug(error, exception);
                return "Excel report creation failed - see log for details";
            }
            finally
            {
                DatabaseBuilder.sqliteConnection.Close();
            }
        }

        private void StartReport(WorkbookPart workbookPart, Sheets sheets)
        {
            try
            {
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                Sheet sheet = new Sheet()
                    {Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetIndex, Name = "STIG Discrepancies"};
                sheetIndex++;
                sheets.Append(sheet);
                _openXmlWriter = OpenXmlWriter.Create(worksheetPart);
                _openXmlWriter.WriteStartElement(new Worksheet());
                WriteReportColumns();
                _openXmlWriter.WriteStartElement(new SheetData());
                _openXmlWriter.WriteElement(new Row() {Hidden = true});
                WriteReportHeaderRow();
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to initialize 'STIG Discrepancies' tab.");
                throw exception;
            }
        }

        private void WriteReportColumns()
        {
            try
            {
                _openXmlWriter.WriteStartElement(new Columns());
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 1, Min = 1});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 2, Min = 2});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 3, Min = 3});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 4, Min = 4});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 5, Min = 5});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 6, Min = 6});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 7, Min = 7});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 8, Min = 8});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 9, Min = 9});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 10, Min = 10});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 11, Min = 11});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 12, Min = 12});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 13, Min = 13});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 14, Min = 14});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 15, Min = 15});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 16, Min = 16});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 24.43, Max = 17, Min = 17});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 18, Min = 18});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 19, Min = 19});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 20, Min = 20});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 21, Min = 21});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 22, Min = 22});
                _openXmlWriter.WriteElement(new Column() {CustomWidth = true, Width = 32.29, Max = 23, Min = 23});
                _openXmlWriter.WriteEndElement();
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to generate 'STIG Discrepancies' columns.");
                throw exception;
            }
        }

        private void WriteReportHeaderRow()
        {
            try
            {
                _openXmlWriter.WriteStartElement(new Row());
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "STIG Name", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "XCCDF STIG Version", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "XCCDF STIG Release", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "CKL STIG Version", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "CKL STIG Release", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "XCCDF Rule ID", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "CKL Rule ID", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "STIG ID", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "Vul (\"V\") ID", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "Group Title", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "Vulnerability Title", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "XCCDF Host", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "CKL Host", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "Raw XCCDF Status", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "Raw CKL Status", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "Mitigated XCCDF Status", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "Mitigated CKL Status", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "CKL Comments", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "CKL Finding Details", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "XCCDF Output", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "XCCDF File Name", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "CKL File Name", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter, "Review Notes", 4, ref sharedStringMaxIndex,
                    sharedStringDictionary);
                _openXmlWriter.WriteEndElement();
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to generate first 'STIG Discrepancies' header row.");
                throw exception;
            }
        }

        private void WriteFindingsToReport()
        {
            try
            {
                using (SQLiteCommand sqliteCommand = DatabaseBuilder.sqliteConnection.CreateCommand())
                {
                    sqliteCommand.Parameters.Add(new SQLiteParameter("UserName",
                        Properties.Settings.Default.ActiveUser));
                    FilterTextCreator filterTextCreator = new FilterTextCreator();
                    string groupFilter = filterTextCreator.Group(sqliteCommand, "SCAP & STIG Discrepancies");
                    string severityFilter = filterTextCreator.Severity(sqliteCommand, "SCAP & STIG Discrepancies");
                    string statusFilter = filterTextCreator.Status(sqliteCommand, "SCAP & STIG Discrepancies");
                    sqliteCommand.CommandText =
                        _ddlReader.ReadDdl(_storedProcedureBase + "Select.StigDiscrepanciesVulnerabilities.dml", assembly);

                    if (!string.IsNullOrWhiteSpace(groupFilter) ||
                        !string.IsNullOrWhiteSpace(severityFilter) ||
                        !string.IsNullOrWhiteSpace(statusFilter))
                    {
                        
                        Regex regex = new Regex(Properties.Resources.RegexSqlGroupBy);
                        sqliteCommand.CommandText =
                            sqliteCommand.CommandText.Insert(regex.Match(sqliteCommand.CommandText).Index, $"{Environment.NewLine}");

                        if (!string.IsNullOrWhiteSpace(groupFilter))
                        {
                            sqliteCommand.CommandText = sqliteCommand.CommandText.Insert(regex.Match(sqliteCommand.CommandText).Index, $"AND {Environment.NewLine}");
                            sqliteCommand.CommandText = sqliteCommand.CommandText.Insert(regex.Match(sqliteCommand.CommandText).Index, groupFilter);
                        }
                        if (!string.IsNullOrWhiteSpace(severityFilter))
                        {
                            sqliteCommand.CommandText = sqliteCommand.CommandText.Insert(regex.Match(sqliteCommand.CommandText).Index, $"AND {Environment.NewLine}");
                            sqliteCommand.CommandText = sqliteCommand.CommandText.Insert(regex.Match(sqliteCommand.CommandText).Index, $"(PrimaryRawRiskIndicator {severityFilter}) ");
                        }
                        sqliteCommand.CommandText = sqliteCommand.CommandText.Insert(regex.Match(sqliteCommand.CommandText).Index, Environment.NewLine);
                        if (!string.IsNullOrWhiteSpace(statusFilter))
                        {
                            regex = new Regex(Properties.Resources.RegexStigDiscrepanciesStatus);
                            MatchCollection matches = regex.Matches(sqliteCommand.CommandText);
                            for (int i = matches.Count - 1; i >= 0; i--)
                            {
                                sqliteCommand.CommandText = sqliteCommand.CommandText.Insert(matches[i].Index, "AND ");
                                sqliteCommand.CommandText = sqliteCommand.CommandText.Insert(matches[i].Index, matches[i].ToString().Contains("Mitigated") ? 
                                    $"MitigatedStatus {statusFilter}" :
                                    $"Status {statusFilter} ");
                            }
                            
                        }
                    }

                    using (SQLiteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                    {
                        while (sqliteDataReader.Read())
                        {
                            if (string.IsNullOrWhiteSpace(sqliteDataReader["XccdfStatus"].ToString()) || 
                                string.IsNullOrWhiteSpace(sqliteDataReader["CklStatus"].ToString()) || 
                                sqliteDataReader["XccdfStatus"].Equals(sqliteDataReader["CklStatus"]))
                            {
                                continue;
                            }

                            WriteFindingToReport(sqliteDataReader);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to write findings to the 'STIG Discrepancies' workbook.");
                throw exception;
            }
        }

        private void WriteFindingToReport(SQLiteDataReader sqliteDataReader)
        {
            try
            {
                _openXmlWriter.WriteStartElement(new Row());
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["SourceName"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);

                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["XccdfSourceVersion"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);

                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["XccdfSourceRelease"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);

                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["CklSourceVersion"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);

                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["CklSourceRelease"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);

                string xccdfRuleId =
                    $"{sqliteDataReader["UniqueVulnerabilityIdentifier"]}r{sqliteDataReader["XccdfVulnVersion"]}";

                if (!string.IsNullOrWhiteSpace(sqliteDataReader["XccdfVulnRelease"].ToString()))
                {
                    xccdfRuleId += $".{sqliteDataReader["XccdfVulnRelease"]}";
                }

                _openXmlCellDataHandler.WriteCellValue(
                    _openXmlWriter,
                    xccdfRuleId,
                    24, ref sharedStringMaxIndex, sharedStringDictionary);

                string cklRuleId =
                    $"{sqliteDataReader["UniqueVulnerabilityIdentifier"]}r{sqliteDataReader["CklVulnVersion"]}";

                if (!string.IsNullOrWhiteSpace(sqliteDataReader["CklVulnRelease"].ToString()))
                {
                    cklRuleId += $".{sqliteDataReader["CklVulnRelease"]}";
                }

                _openXmlCellDataHandler.WriteCellValue(
                    _openXmlWriter,
                    cklRuleId,
                    24, ref sharedStringMaxIndex, sharedStringDictionary);

                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["SecondaryVulnerabilityIdentifier"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["VulnerabilityGroupIdentifier"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["VulnerabilityGroupTitle"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["VulnerabilityTitle"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);

                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["XccdfHost"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["CklHost"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["XccdfStatus"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["CklStatus"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["MitigatedXccdfStatus"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["MitigatedCklStatus"].ToString(), 24,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["CklComments"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["CklFindingDetails"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["XccdfOutput"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["XccdfFileName"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    sqliteDataReader["CklFileName"].ToString(), 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);
                
                _openXmlCellDataHandler.WriteCellValue(_openXmlWriter,
                    string.Empty, 20,
                    ref sharedStringMaxIndex, sharedStringDictionary);

                _openXmlWriter.WriteEndElement();
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to write finding to 'STIG Discrepancies'.");
                throw exception;
            }
        }

        private void EndReport()
        {
            try
            {
                _openXmlWriter.WriteEndElement();
                _openXmlWriter.WriteEndElement();
                _openXmlWriter.Dispose();
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to finalize 'STIG Discrepancies' tab.");
                throw exception;
            }
        }
    }
}