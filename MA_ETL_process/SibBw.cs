using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MA_ETL_process
{
    internal abstract class SibBw
    {
        public Dictionary<string, string> stringValues = new Dictionary<string, string>();
        public Dictionary<string, string> numberValues = new Dictionary<string, string>();
        public Dictionary<string, string> dateValues = new Dictionary<string, string>();

        public abstract string GetCypherCreate();

        protected string GetCypherCreate(
           string cypherIdentifier, string label, 
           KeyValuePair<string, string> cypherIdentifierCustom = new KeyValuePair<string, string>())
        {
            // example query from neo4j-docs to get the syntax
            // CREATE (a:Person {name:'Brie Larson', born:1989})
            // initialize CREATE
            string cypher = $"CREATE (:{label} {{";

            // add identifier as required property
            cypher += $"identifier:'{cypherIdentifier}', ";

            // if existing, add secondary custom identifier; e.g. SCHADALT gets an identifier of the corresponding PRUFALT
            if (cypherIdentifierCustom.Key != null)
            {
                cypher += $"{cypherIdentifierCustom.Key}:'{cypherIdentifierCustom.Value}', ";
            }

            // add numberValues as properties
            foreach (KeyValuePair<string, string> kvp in numberValues)
            {
                cypher += $"{kvp.Key}:{kvp.Value}, ";
            }

            // add stringValues as properties
            foreach (KeyValuePair<string, string> kvp in stringValues)
            {
                cypher += $"{kvp.Key}:'{kvp.Value.Replace("'", "\\'")}', ";
                //...Replace("'", "\\'")  - adds one backslash before apostrophes inside the string
                //                       to mark them as part of the string and not as end of string
                //kvp.Value.TrimEnd() // removes all whitespaces at the end of the string (already done in SqlClient)
            }

            // add dateValues as properties
            foreach (KeyValuePair<string, string> kvp in dateValues)
            {
                cypher += $"{kvp.Key}:localdatetime('{kvp.Value}'), ";
            }

            // remove last ", " in the cypher-string
            cypher = cypher.Remove(cypher.Length - 2, 1) ;

            // close properties-parenthes and CREATE-parenthesis; return string
            return cypher += "})";
        }
    }

    internal static class static_SibBW
    {
        public static string GetCypherConstraintKey(string label)
        {
            // CREATE CONSTRAINT constraint_name FOR (n:Label) REQUIRE n.property IS NODE KEY
            return $"CREATE CONSTRAINT keyConstraint_{label} IF NOT EXISTS FOR (n:{label}) REQUIRE (n.identifier) IS NODE KEY";
        }
    }

    internal class SibBW_GES_BW : SibBw
    {
        public string identifier { get { return ("BWNR" + stringValues["BWNR"]).Replace(" ", "_"); } }

        public override string GetCypherCreate()
        {
            return GetCypherCreate(identifier, static_SibBW_GES_BW.label);
        }
    }

    internal static class static_SibBW_GES_BW
    {
        public static string label = "GES_BW";
        public static string GetSqlQuery(string bridgeNumber)
        {
            // ... SELECT TOP (100) [BWNR], ...
            return "SELECT [BWNR], [BWNAME], [LAENGE_BR]\n" +
                "FROM [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW]\n" +
                "WHERE [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW].[BWNR]\n" +
                $"IN ('{bridgeNumber}')";
        }
        public static string GetCypherMergeToTeilBW()
        {
            return "MATCH (bw:GES_BW)\r\n" +
                "MATCH (teilBw:TEIL_BW) WHERE bw.BWNR = teilBw.BWNR\r\n" +
                "MERGE (bw)-[r:bw_teilBw]->(teilBw)\r\n" +
                "RETURN count(r)";
        }
    }

    internal class SibBW_TEIL_BW : SibBw
    {
        public string identifier { get { return ("ID_NR" + stringValues["ID_NR"]).Replace(" ", "_"); } }

        public override string GetCypherCreate()
        {
            return GetCypherCreate(identifier, static_SibBW_TEIL_BW.label);
        }
    }

    internal static class static_SibBW_TEIL_BW
    {
        public static string label = "TEIL_BW";
        public static string GetSqlQuery(string bridgeNumber)
        {
            return "SELECT [BWNR], [TEIL_BWNR], [ID_NR], [TW_NAME], [KONSTRUKT], [STADIUM]\n" +
                "FROM [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW]\n" +
                "WHERE [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW].[BWNR]\n" +
                $"IN('{bridgeNumber}')";
        }
        public static string GetCypherMergeToPRUFALT()
        {
            return "MATCH (teilBw:TEIL_BW)\r\n" +
                "MATCH (prufAlt:PRUFALT) WHERE teilBw.ID_NR = prufAlt.ID_NR\r\n" +
                "MERGE (teilBw)-[r:teilBw_prufAlt]->(prufAlt)\r\n" +
                "RETURN count(r)";
        }
    }

    internal class SibBW_PRUFALT : SibBw
    {
        public string identifier { get { return ("ID_NR" + stringValues["ID_NR"] + 
                    "_" + numberValues["PRUFJAHR"] + "_" + stringValues["PRUFART"]).Replace(" ", "_"); } }

        public override string GetCypherCreate()
        {
            return GetCypherCreate(identifier, static_SibBW_PRUFALT.label);
        }
    }

    internal static class static_SibBW_PRUFALT
    {
        public static string label = "PRUFALT";
        public static string GetSqlQuery(string bridgeNumber)
        {
            // all DB-entries: "SELECT [ID_NR], [BWNR], [TEIL_BWNR], [IBWNR], [AMT], [PRUFART], [PRUFJAHR], [DIENSTSTEL], [PRUEFER], "[PRUFDAT1], [PRUFDAT2], [PRUFRICHT], [PRUFTEXT], [UBERDAT], [BEARBDAT], [ER_ZUSTAND], [ZS_MINTRAG], [FESTLEGTXT], "[MASSNAHME], [IDENT], [MAX_S], [MAX_V], [MAX_D], [DAT_NAE_H], [ART_NAE_H], [DAT_NAE_S], [DAT_NAE_E]"
            // first selection: [ID_NR], [BWNR], [TEIL_BWNR], [AMT], [PRUFART], [PRUFJAHR], [PRUFDAT1], [PRUFDAT2], [ER_ZUSTAND], [ZS_MINTRAG], [IDENT], [MAX_S], [MAX_V], [MAX_D]
            return "SELECT [ID_NR], [BWNR], [TEIL_BWNR], [PRUFART], [PRUFJAHR], " +
                "[PRUFDAT2], [ER_ZUSTAND], [MAX_S], [MAX_V], [MAX_D]\n" +
                "FROM[SIB_BAUWERKE_19_20230427].[dbo].[PRUFALT]\n" +
                "WHERE[SIB_BAUWERKE_19_20230427].[dbo].[PRUFALT].[BWNR]\n" +
                $"IN('{bridgeNumber}')";
        }
        public static string GetCypherMergeToSCHADALT()
        {
            return "MATCH (prufAlt:PRUFALT)\r\n" +
                "MATCH (schadAlt:SCHADALT) WHERE prufAlt.identifier = schadAlt.identifierPruf\r\n" +
                "MERGE (prufAlt)-[r:prufAlt_schadAlt]->(schadAlt)\r\n" +
                "RETURN count(r)";
        }
    }

    internal class SibBW_SCHADALT : SibBw
    {
        public string identifier { get {
                // ID_NR, PRUFJAHR, PRA (=Prüfart: {E, H})
                return ("ID_NR" + stringValues["ID_NR"] + "_" + numberValues["PRUFJAHR"] 
                    + "_" + stringValues["PRA"] + "_" + stringValues["IDENT"]).Replace(" ", "_");
            }
        }
        public string identifierPruf
        {
            get
            {
                // ID_NR, PRUFJAHR, PRA (=Prüfart: {E, H})
                return ("ID_NR" + stringValues["ID_NR"].Replace(" ", "_") +
                    "_" + numberValues["PRUFJAHR"] + "_" + stringValues["PRA"]).Replace(" ", "_");
            }
        }

        public override string GetCypherCreate()
        {
            return GetCypherCreate(identifier, static_SibBW_SCHADALT.label, KeyValuePair.Create("identifierPruf", identifierPruf));
        }
    }

    internal static class static_SibBW_SCHADALT
    {
        public static string label = "SCHADALT";
        public static string GetSqlQuery(string bridgeNumber)
        {
            // SchadenAlt hängt an Prüfung via ID_NR, PRUFJAHR, PRA (=Prüfart: {E, H})
            // zur eindeutige Identifizierung des Schadens: LFDNR und SCHAD_ID sind nicht konsistent; Bedeutung IDENT ist unklar
            // all DB-entries: SELECT [ID_NR], [LFDNR], [BAUTEIL], [KONTEIL], [ZWGRUPPE], [SCHADEN], [SCHADEN_M], [MENGE_ALL], [MENGE_DI], [MENGE_DI_M], [UEBERBAU], [UEBERBAU_M], [FELD], [FELD_M], [LAENGS], [LAENGS_M], [QUER], [QUER_M], [HOCH], [HOCH_M], [BEWERT_D], [BEWERT_V], [BEWERT_S], [S_VERAEND], [BEMERK1], [BEMERK1_M], [BEMERK2], [BEMERK2_M], [BEMERK3], [BEMERK3_M], [BEMERK4], [BEMERK4_M], [BEMERK5], [BEMERK5_M], [BEMERK6], [BEMERK6_M], [BWNR], [TEIL_BWNR], [IBWNR], [IDENT], [AMT], [PRUFJAHR], [PRA], [TEXT], [BILD], [KONT_JN], [NOT_KONST], [KONVERT], [SCHAD_ID], [BSP_ID], [BAUTLGRUP], [DETAILKONT]
            // first selection: [ID_NR], [LFDNR], [BAUTEIL], [KONTEIL], [ZWGRUPPE], [SCHADEN], [MENGE_ALL], [MENGE_DI], [MENGE_DI_M], [UEBERBAU], [FELD], [FELD_M], [LAENGS], [LAENGS_M], [QUER], [QUER_M], [HOCH], [BEWERT_D], [BEWERT_V], [BEWERT_S], [S_VERAEND], [BEMERK1], [BEMERK1_M], [BWNR], [TEIL_BWNR], [IBWNR], [IDENT], [AMT], [PRUFJAHR], [PRA], [KONT_JN], [NOT_KONST], [KONVERT], [SCHAD_ID], [BSP_ID], [BAUTLGRUP], [DETAILKONT]
            return "SELECT [ID_NR], [BWNR], [TEIL_BWNR], [IDENT], [PRUFJAHR], [PRA], " +
                "[BAUTEIL], [KONTEIL], [ZWGRUPPE], [SCHADEN], " +
                "[BEWERT_D], [BEWERT_V], [BEWERT_S], [S_VERAEND]\n" +
                "FROM[SIB_BAUWERKE_19_20230427].[dbo].[SCHADALT]\n" +
                "WHERE[SIB_BAUWERKE_19_20230427].[dbo].[SCHADALT].[BWNR]\n" +
                $"IN('{bridgeNumber}')";
        }
    }
}
