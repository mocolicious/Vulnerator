﻿using log4net;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Vulnerator.Model.DataAccess;
using Vulnerator.Helper;
using Vulnerator.Model.Object;

namespace Vulnerator.Model.BusinessLogic
{
    /// <summary>
    /// Class housing all items required to parse DISA STIG *.ckl scan files.
    /// </summary>
    public class CklReader
    {
        DatabaseInterface databaseInterface = new DatabaseInterface();
        private object techArea = string.Empty;
        private object webDbSite = string.Empty;
        private object webDbInstance = string.Empty;
        private object classification = string.Empty;
        private List<string> ccis = new List<string>();
        private DateTime dateTimeNow = DateTime.Now;
        string _groupName = null;
        private string[] persistentParameters =  {
            "GroupName", "FindingSourceFileName", "SourceName", "SourceVersion", "SourceRelease", "DiscoveredHostName", "ScanIP", "FQDN", "NetBIOS", "FindingType", "VulnerabilityRelease", "PublishedDate"
        };

        /// <summary>
        /// Reads *.ckl files exported from the DISA STIG Viewer and writes the results to the appropriate DataTables.
        /// </summary>
        /// <param name="fileName">Name of *.ckl file to be parsed.</param>
        /// <param name="mitigationsList">List of mitigation items for vulnerabilities to be read against.</param>
        /// <param name="systemName">Name of the system that the mitigations check will be run against.</param>
        /// <returns>string Value</returns>
        public string ReadCklFile(File file, string groupName)
        {
            try
            {
                if (file.FilePath.IsFileInUse())
                {
                    LogWriter.LogError($"'{file.FileName}' is in use; please close any open instances and try again.");
                    return "Failed; File In Use";
                }
                if (DatabaseBuilder.sqliteConnection.State.ToString().Equals("Closed"))
                { DatabaseBuilder.sqliteConnection.Open(); }
                using (SQLiteTransaction sqliteTransaction = DatabaseBuilder.sqliteConnection.BeginTransaction())
                {
                    using (SQLiteCommand sqliteCommand = DatabaseBuilder.sqliteConnection.CreateCommand())
                    {
                        databaseInterface.InsertParameterPlaceholders(sqliteCommand);
                        sqliteCommand.Parameters["GroupName"].Value = string.IsNullOrWhiteSpace(_groupName) ? "All" : _groupName;
                        sqliteCommand.Parameters["LastObserved"].Value = dateTimeNow;
                        sqliteCommand.Parameters["VulnerabilityRelease"].Value = string.Empty;
                        databaseInterface.InsertParsedFileSource(sqliteCommand, file);
                        XmlReaderSettings xmlReaderSettings = GenerateXmlReaderSettings();
                        using (XmlReader xmlReader = XmlReader.Create(file.FilePath, xmlReaderSettings))
                        {
                            while (xmlReader.Read())
                            {
                                if (xmlReader.IsStartElement())
                                {
                                    switch (xmlReader.Name)
                                    {
                                        case "ASSET":
                                            {
                                                ParseAssetNode(xmlReader, sqliteCommand);
                                                break;
                                            }
                                        case "STIG_INFO":
                                            {
                                                ParseStigInfoNode(xmlReader, sqliteCommand);
                                                break;
                                            }
                                        case "VULN":
                                            {
                                                ParseVulnNode(xmlReader, sqliteCommand);
                                                break;
                                            }
                                    }
                                }
                            }
                        }
                        databaseInterface.DeleteRemovedVulnerabilities(sqliteCommand);
                    }
                    sqliteTransaction.Commit();
                }
                return "Processed";
            }
            catch (Exception exception)
            {
                string error = $"Unable to process CKL file '{file.FileName}'.";
                LogWriter.LogErrorWithDebug(error, exception);
                return "Failed; See Log";
            }
            finally
            { DatabaseBuilder.sqliteConnection.Close(); }
        }

        private void ParseAssetNode(XmlReader xmlReader, SQLiteCommand sqliteCommand)
        {
            try
            {
                object ip = string.Empty;
                object mac = string.Empty;
                while (xmlReader.Read())
                {
                    if (xmlReader.IsStartElement())
                    {
                        switch (xmlReader.Name)
                        {
                            case "ROLE":
                                {
                                    sqliteCommand.Parameters["Role"].Value = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                            case "HOST_NAME":
                                {
                                    object hostName = xmlReader.ObtainCurrentNodeValue(true);
                                    if (!string.IsNullOrWhiteSpace(hostName.ToString()) && !hostName.Equals("HOST NAME"))
                                    {
                                        sqliteCommand.Parameters["DiscoveredHostName"].Value = hostName;
                                        sqliteCommand.Parameters["DisplayedHostName"].Value = hostName;
                                    }
                                    break;
                                }
                            case "HOST_IP":
                                {
                                    ip = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                            case "HOST_MAC":
                                {
                                    mac = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                            case "HOST_FQDN":
                                {
                                    sqliteCommand.Parameters["FQDN"].Value = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                            case "TECH_AREA":
                                {
                                    techArea = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                            case "WEB_DB_SITE":
                                {
                                    webDbSite = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                            case "WEB_DB_INSTANCE":
                                {
                                    webDbInstance = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                        }
                    }
                    else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name.Equals("ASSET"))
                    {
                        sqliteCommand.Parameters["IsVirtualServer"].Value = "False";
                        databaseInterface.InsertHardware(sqliteCommand);
                        databaseInterface.MapHardwareToGroup(sqliteCommand);
                        if (!string.IsNullOrWhiteSpace(ip.ToString()) && !ip.Equals("IP"))
                        { ParseIpAndMacAddress(sqliteCommand, ip.ToString()); }
                        if (!string.IsNullOrWhiteSpace(mac.ToString()) && !mac.Equals("MAC"))
                        { ParseIpAndMacAddress(sqliteCommand, mac.ToString()); }
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to parse 'ASSET' node.");
                throw exception;
            }
        }

        private void ParseIpAndMacAddress(SQLiteCommand sqliteCommand, string parameter)
        {
            try
            {
                string[] regexArray = new string[] { Properties.Resources.RegexIPv4, Properties.Resources.RegexIPv6, Properties.Resources.RegexMAC };
                foreach (string expression in regexArray)
                {
                    Regex regex = new Regex(expression);
                    foreach (Match match in regex.Matches(parameter))
                    {
                        switch (Array.IndexOf(regexArray, expression))
                        {
                            case 0:
                                {
                                    PrepareIpAndMacAddress(sqliteCommand, match.ToString(), "IP_Addresses");
                                    break;
                                }
                            case 1:
                                {
                                    PrepareIpAndMacAddress(sqliteCommand, match.ToString(), "IP_Addresses");
                                    break;
                                }
                            case 2:
                                {
                                    PrepareIpAndMacAddress(sqliteCommand, match.ToString(), "MAC_Addresses");
                                    break;
                                }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to parse IP / MAC Address.");
                throw exception;
            }
        }

        private void PrepareIpAndMacAddress(SQLiteCommand sqliteCommand, string item, string table)
        {
            try
            {
                if (table.Equals("IP_Addresses"))
                {
                    sqliteCommand.Parameters["IP_Address"].Value = item;
                    databaseInterface.InsertAndMapIpAddress(sqliteCommand);
                }
                else
                {
                    sqliteCommand.Parameters["MAC_Address"].Value = item;
                    databaseInterface.InsertAndMapMacAddress(sqliteCommand);
                }
            }
            catch (Exception exception)
            {
                LogWriter.LogError($"Unable to insert IP / MAC address '{item}' into database.");
                throw exception;
            }
        }

        private void ParseStigInfoNode(XmlReader xmlReader, SQLiteCommand sqliteCommand)
        {
            try
            {
                xmlReader.Read();
                if (xmlReader.Name.Equals("STIG_TITLE"))
                {
                    string sourceName = xmlReader.ObtainCurrentNodeValue(true).ToString().Replace('_', ' ');
                    sourceName = sourceName.ToSanitizedSource();
                    sqliteCommand.Parameters["SourceName"].Value = sourceName;
                    databaseInterface.InsertVulnerabilitySource(sqliteCommand);
                }
                else
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.IsStartElement() && xmlReader.Name.Equals("SID_NAME"))
                        {
                            xmlReader.Read();
                            switch (xmlReader.Value)
                            {
                                case "title":
                                    {
                                        string sourceName = ObtainStigInfoSubNodeValue(xmlReader).Replace('_', ' ');
                                        sourceName = sourceName.ToSanitizedSource();
                                        sqliteCommand.Parameters["SourceName"].Value = sourceName;
                                        break;
                                    }
                                case "version":
                                    {
                                        string rawVersion = ObtainStigInfoSubNodeValue(xmlReader);
                                        if (!rawVersion.Contains('.'))
                                        {
                                            sqliteCommand.Parameters["SourceVersion"].Value = rawVersion;
                                        }
                                        else
                                        {
                                            Regex regex = new Regex(Properties.Resources.RegexStigVersion);
                                            sqliteCommand.Parameters["SourceVersion"].Value =
                                                regex.Match(rawVersion).Value;
                                            regex = new Regex(Properties.Resources.RegexStigRelease);
                                            sqliteCommand.Parameters["SourceRelease"].Value =
                                                regex.Match(rawVersion).Value;
                                        }
                                        break;
                                    }
                                case "releaseinfo":
                                    {
                                        string release = ObtainStigInfoSubNodeValue(xmlReader);
                                        Regex regex = new Regex(Properties.Resources.RegexStigDate);
                                        sqliteCommand.Parameters.Add(new SQLiteParameter("PublishedDate", DateTime
                                            .ParseExact(regex.Match(release).ToString(), "dd MMM yyyy", CultureInfo.InvariantCulture)
                                            .ToShortDateString()));
                                        if (string.IsNullOrWhiteSpace(sqliteCommand.Parameters["SourceRelease"].Value
                                            .ToString()))
                                        { sqliteCommand.Parameters["SourceRelease"].Value = release.Contains(" ") ? release.Split(' ')[1] : release; }
                                        break;
                                    }
                                case "stigid":
                                    {
                                        sqliteCommand.Parameters["SourceSecondaryIdentifier"].Value = ObtainStigInfoSubNodeValue(xmlReader);
                                        break;
                                    }
                                case "description":
                                    {
                                        sqliteCommand.Parameters["SourceDescription"].Value = ObtainStigInfoSubNodeValue(xmlReader);
                                        break;
                                    }
                                case "filename":
                                    {
                                        sqliteCommand.Parameters["VulnerabilitySourceFileName"].Value = ObtainStigInfoSubNodeValue(xmlReader);
                                        break;
                                    }
                                default:
                                    { break; }
                            }
                        }
                        else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name.Equals("STIG_INFO"))
                        {
                            databaseInterface.UpdateVulnerabilitySource(sqliteCommand);
                            databaseInterface.InsertVulnerabilitySource(sqliteCommand);
                            return;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to parse 'STIG_INFO' node.");
                throw exception;
            }
        }

        private void ParseVulnNode(XmlReader xmlReader, SQLiteCommand sqliteCommand)
        {
            try
            {
                sqliteCommand.Parameters["ModifiedDate"].Value = DateTime.Now.ToShortDateString();
                while (xmlReader.Read())
                {
                    if (xmlReader.IsStartElement() && xmlReader.Name.Equals("VULN_ATTRIBUTE"))
                    {
                        xmlReader.Read();
                        switch (xmlReader.Value)
                        {
                            case "Vuln_Num":
                                {
                                    sqliteCommand.Parameters["VulnerabilityGroupIdentifier"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Severity":
                                {
                                    sqliteCommand.Parameters["PrimaryRawRiskIndicator"].Value = ObtainAttributeDataNodeValue(xmlReader).ToRawRisk();
                                    break;
                                }
                            case "Group_Title":
                                {
                                    sqliteCommand.Parameters["VulnerabilityGroupTitle"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Rule_ID":
                                {
                                    string rule = ObtainAttributeDataNodeValue(xmlReader);
                                    string ruleRelease = string.Empty;
                                    if (rule.Contains("_"))
                                    { rule = rule.Split('_')[0]; }
                                    if (rule.Contains("r"))
                                    {
                                        ruleRelease = rule.Split('r')[1];
                                        rule = rule.Split('r')[0];
                                    }
                                    sqliteCommand.Parameters["UniqueVulnerabilityIdentifier"].Value = rule;
                                    sqliteCommand.Parameters["VulnerabilityVersion"].Value = ruleRelease;
                                    break;
                                }
                            case "Rule_Ver":
                                {
                                    sqliteCommand.Parameters["SecondaryVulnerabilityIdentifier"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Rule_Title":
                                {
                                    sqliteCommand.Parameters["VulnerabilityTitle"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Vuln_Discuss":
                                {
                                    sqliteCommand.Parameters["VulnerabilityDescription"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "CheckContent":
                                {
                                    sqliteCommand.Parameters["CheckContent"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Check_Content":
                                {
                                    sqliteCommand.Parameters["CheckContent"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "FixText":
                                {
                                    sqliteCommand.Parameters["FixText"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Fix_Text":
                            {
                                sqliteCommand.Parameters["FixText"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                break;
                            }
                            case "FalsePositives":
                                {
                                    sqliteCommand.Parameters["FalsePositives"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "False_Positives":
                                {
                                    sqliteCommand.Parameters["FalsePositives"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "FalseNegatives":
                                {
                                    sqliteCommand.Parameters["FalseNegatives"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "False_Negatives":
                                {
                                    sqliteCommand.Parameters["FalseNegatives"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Documentable":
                                {
                                    sqliteCommand.Parameters["IsDocumentable"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Mitigations":
                                {
                                    sqliteCommand.Parameters["Mitigations"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "PotentialImpacts":
                                {
                                    sqliteCommand.Parameters["PotentialImpacts"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Potential_Impact":
                                {
                                    sqliteCommand.Parameters["PotentialImpacts"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "ThirdPartyTools":
                                {
                                    sqliteCommand.Parameters["ThirdPartyTools"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Third_Party_Tools":
                                {
                                    sqliteCommand.Parameters["ThirdPartyTools"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "MitigationControl":
                                {
                                    sqliteCommand.Parameters["MitigationControl"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Mitigation_Control":
                                {
                                    sqliteCommand.Parameters["MitigationControl"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Responsibility":
                                {
                                    break;
                                }
                            case "SecurityOverrideGuidance":
                                {
                                    sqliteCommand.Parameters["SecurityOverrideGuidance"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                            case "Security_Override_Guidance":
                            {
                                sqliteCommand.Parameters["SecurityOverrideGuidance"].Value = ObtainAttributeDataNodeValue(xmlReader);
                                break;
                            }
                            case "CCI_REF":
                                {
                                    ccis.Add(ObtainAttributeDataNodeValue(xmlReader).Replace("CCI-", string.Empty));
                                    break;
                                }
                            case "Class":
                                {
                                    classification = ObtainAttributeDataNodeValue(xmlReader);
                                    break;
                                }
                        }
                    }
                    else if (xmlReader.IsStartElement() && xmlReader.Name.Equals("STATUS"))
                    {
                        sqliteCommand.Parameters["InstanceIdentifier"].Value =
                            $"{sqliteCommand.Parameters["DiscoveredHostName"].Value}_" +
                            $"{sqliteCommand.Parameters["UniqueVulnerabilityIdentifier"].Value}r{sqliteCommand.Parameters["VulnerabilityVersion"].Value}_" +
                            "CKL";
                        switch (databaseInterface.CompareVulnerabilityVersions(sqliteCommand))
                        {
                            case "Record Not Found":
                            {
                                sqliteCommand.Parameters["FirstDiscovered"].Value = dateTimeNow;
                                    databaseInterface.InsertVulnerability(sqliteCommand);
                                    databaseInterface.MapVulnerabilityToSource(sqliteCommand);
                                    break;
                                }
                            case "Ingested Version Is Newer":
                                {
                                    databaseInterface.InsertVulnerability(sqliteCommand);
                                    databaseInterface.MapVulnerabilityToSource(sqliteCommand);
                                    List<string> ids = databaseInterface.SelectOutdatedVulnerabilities(sqliteCommand, false);
                                    sqliteCommand.Parameters.Add(new SQLiteParameter("UpdatedStatus", "Completed"));
                                    foreach (string id in ids)
                                    {
                                        sqliteCommand.Parameters.Add(new SQLiteParameter("UniqueFinding_ID", id));
                                        databaseInterface.UpdateUniqueFindingStatusById(sqliteCommand);
                                    }
                                    if (sqliteCommand.Parameters.Contains("UpdatedStatus"))
                                    { sqliteCommand.Parameters.Remove(sqliteCommand.Parameters["UpdatedStatus"]); }
                                    if (sqliteCommand.Parameters.Contains("UniqueFinding_ID"))
                                    { sqliteCommand.Parameters.Remove(sqliteCommand.Parameters["UniqueFinding_ID"]); }
                                    break;
                                }
                            case "Existing Version Is Newer":
                                {
                                    databaseInterface.UpdateVulnerabilityDates(sqliteCommand);
                                    sqliteCommand.Parameters["DeltaAnalysisIsRequired"].Value = "True";
                                    databaseInterface.UpdateDeltaAnalysisFlags(sqliteCommand);
                                    break;
                                }
                            case "Identical Versions":
                                {
                                    databaseInterface.UpdateVulnerabilityDates(sqliteCommand);
                                    break;
                                }
                        }
                        ParseUniqueFindingData(sqliteCommand, xmlReader);
                        if (ccis.Count > 0)
                        {
                            foreach (string cci in ccis)
                            {
                                if (string.IsNullOrWhiteSpace(cci))
                                { continue; }
                                sqliteCommand.Parameters["CCI_Number"].Value = cci;
                                databaseInterface.MapVulnerabilityToCci(sqliteCommand);
                                sqliteCommand.Parameters["CCI_Number"].Value = string.Empty;
                            }
                            ccis.Clear();
                        }

                        foreach (SQLiteParameter parameter in sqliteCommand.Parameters)
                        {
                            if (!persistentParameters.Contains(parameter.ParameterName))
                            { parameter.Value = DBNull.Value; }
                        }
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to parse 'VULN' node.");
                throw exception;
            }
        }

        private void ParseUniqueFindingData(SQLiteCommand sqliteCommand, XmlReader xmlReader)
        {
            try
            {
                xmlReader.Read();
                sqliteCommand.Parameters["Status"].Value = xmlReader.Value.ToVulneratorStatus();
                while (xmlReader.Read())
                {
                    if (xmlReader.IsStartElement())
                    {
                        switch (xmlReader.Name)
                        {
                            case "FINDING_DETAILS":
                                {
                                    object nodeValue = xmlReader.ObtainCurrentNodeValue(true);
                                    if (nodeValue == DBNull.Value)
                                    {
                                        sqliteCommand.Parameters["ToolGeneratedOutput"].Value = DBNull.Value;
                                        sqliteCommand.Parameters["FindingDetails"].Value = DBNull.Value;
                                    }
                                    else if (nodeValue.ToString().Contains("<cdf:"))
                                    {
                                        sqliteCommand.Parameters["ToolGeneratedOutput"].Value = nodeValue;
                                        sqliteCommand.Parameters["FindingDetails"].Value = DBNull.Value;
                                    }
                                    else
                                    {
                                        sqliteCommand.Parameters["FindingDetails"].Value = nodeValue;
                                        sqliteCommand.Parameters["ToolGeneratedOutput"].Value = DBNull.Value;
                                    }
                                    break;
                                }
                            case "COMMENTS":
                                {
                                    sqliteCommand.Parameters["Comments"].Value = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                            case "SEVERITY_OVERRIDE":
                                {
                                    sqliteCommand.Parameters["SeverityOverride"].Value = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                            case "SEVERITY_JUSTIFICATION":
                                {
                                    sqliteCommand.Parameters["SeverityOverrideJustification"].Value = xmlReader.ObtainCurrentNodeValue(true);
                                    break;
                                }
                        }
                    }
                    else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name.Equals("VULN"))
                    {
                        PrepareUniqueFinding(sqliteCommand);
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to parse unique finding data.");
                throw exception;
            }
        }

        private void PrepareUniqueFinding(SQLiteCommand sqliteCommand)
        {
            try
            {
                sqliteCommand.Parameters["TechnologyArea"].Value = techArea;
                sqliteCommand.Parameters["WebDB_Site"].Value = webDbSite;
                sqliteCommand.Parameters["WebDB_Instance"].Value = webDbInstance;
                sqliteCommand.Parameters["LastObserved"].Value = DateTime.Now;
                if (string.IsNullOrWhiteSpace(sqliteCommand.Parameters["DeltaAnalysisIsRequired"].Value.ToString()))
                { sqliteCommand.Parameters["DeltaAnalysisIsRequired"].Value = "False"; }
                sqliteCommand.Parameters["FirstDiscovered"].Value = DateTime.Now;
                sqliteCommand.Parameters["Classification"].Value = classification;
                sqliteCommand.Parameters["FindingType"].Value = "CKL";
                databaseInterface.UpdateUniqueFinding(sqliteCommand);
                databaseInterface.InsertUniqueFinding(sqliteCommand);
            }
            catch (Exception exception)
            {
                LogWriter.LogError(
                    $"Unable to create a Unique Finding record for plugin '{sqliteCommand.Parameters["UniqueVulnerabilityIdentifier"].Value}'.");
                throw exception;
            }
        }

        private XmlReaderSettings GenerateXmlReaderSettings()
        {
            try
            {
                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                xmlReaderSettings.IgnoreWhitespace = true;
                xmlReaderSettings.IgnoreComments = true;
                xmlReaderSettings.ValidationType = ValidationType.Schema;
                xmlReaderSettings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ProcessInlineSchema;
                xmlReaderSettings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ProcessSchemaLocation;
                return xmlReaderSettings;
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to generate XmlReaderSettings.");
                throw exception;
            }
        }

        private string ObtainStigInfoSubNodeValue(XmlReader xmlReader)
        {
            try
            {
                while (xmlReader.Read())
                {
                    if (xmlReader.IsStartElement() && xmlReader.Name.Equals("SID_DATA"))
                    {
                        xmlReader.Read();
                        return xmlReader.Value;
                    }
                    if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name.Equals("SI_DATA"))
                    { return string.Empty; }
                }
                return "Read too far!";
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to obtain the value from the 'STIG_INFO' sub-node");
                throw exception;
            }
        }

        private string ObtainAttributeDataNodeValue(XmlReader xmlReader)
        {
            try
            {
                while (!xmlReader.Name.Equals("ATTRIBUTE_DATA"))
                { xmlReader.Read(); }
                xmlReader.Read();
                string value = xmlReader.Value;
                value = value.Replace("&gt", ">");
                value = value.Replace("&lt", "<");
                return value;
            }
            catch (Exception exception)
            {
                LogWriter.LogError("Unable to obtain 'ATTRIBUTE_DATA' node value.");
                throw exception;
            }
        }

        public File ObtainIdentifiers(File file)
        {
            try
            {
                XmlReaderSettings xmlReaderSettings = GenerateXmlReaderSettings();
                using (XmlReader xmlReader = XmlReader.Create(file.FilePath, xmlReaderSettings))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.IsStartElement())
                        {
                            switch (xmlReader.Name)
                            {
                                case "HOST_NAME":
                                    {
                                        file.SetFileHostName(xmlReader.ObtainCurrentNodeValue(true).ToString());
                                        break;
                                    }
                                case "HOST_IP":
                                    {
                                        file.SetFileIpAddress(xmlReader.ObtainCurrentNodeValue(true).ToString());
                                        break;
                                    }
                                case "HOST_MAC":
                                    {
                                        file.SetFileMacAddress(xmlReader.ObtainCurrentNodeValue(true).ToString());
                                        break;
                                    }
                                default:
                                    { break; }
                            }
                        }
                        else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name.Equals("ASSET"))
                        { break; }
                    }
                }
                bool hostname = IdentifierRequired("DiscoveredHostName", file);
                bool ipAddress = IdentifierRequired("IpAddress", file);
                bool macAddress = IdentifierRequired("MacAddress", file);
                if (!hostname || !ipAddress || !macAddress)
                { file.IdentifiersProvided = "False"; }
                return file;
            }
            catch (Exception exception)
            {
                string error = $"Unable to verify host '{file.FileHostName}' - '{file.FileIpAddress}' exists.";
                LogWriter.LogErrorWithDebug(error, exception);
                return file;
            }
        }

        private bool IdentifierRequired(string fieldName, File file)
        {
            bool value = true;
            switch (fieldName)
            {
                case "DiscoveredHostName":
                    {
                        if (Properties.Settings.Default.CklRequiresHostName)
                        {
                            if (string.IsNullOrWhiteSpace(file.FileHostName) || file.FileHostName.Contains("HOST NAME"))
                            { value = false; }
                        }
                        break;
                    }
                case "IpAddress":
                    {
                        if (Properties.Settings.Default.CklRequiresIpAddress)
                        {
                            if (string.IsNullOrWhiteSpace(file.FileIpAddress) || file.FileIpAddress.Contains("IP"))
                            { value = false; }
                        }
                        break;
                    }
                case "MacAddress":
                    {
                        if (Properties.Settings.Default.CklrequiresMacAddress)
                        {
                            if (string.IsNullOrWhiteSpace(file.FileMacAddress) || file.FileMacAddress.Contains("MAC"))
                            { value = false; }
                        }
                        break;
                    }
                default:
                    { break; }
            }
            return value;
        }
    }
}
