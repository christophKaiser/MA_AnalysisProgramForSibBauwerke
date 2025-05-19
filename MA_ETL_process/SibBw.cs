using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MA_ETL_process
{
    internal class SibBw
    {
        public Dictionary<string, string> stringValues = new Dictionary<string, string>();
        public Dictionary<string, string> numberValues = new Dictionary<string, string>();
        public Dictionary<string, string> dateValues = new Dictionary<string, string>();

        public string GetCypherConstraintKey(string label)
        {
            // CREATE CONSTRAINT constraint_name FOR (n:Label) REQUIRE n.property IS NODE KEY
            return $"CREATE CONSTRAINT keyConstraint_{label} FOR (n:{label}) REQUIRE (n.identifier) IS NODE KEY";
        }

        protected string GetCypherCreate(
           string cypherIdentifier, string lable, 
           KeyValuePair<string, string> cypherIdentifierCustom = new KeyValuePair<string, string>())
        {
            // CREATE (a:Person {name:'Brie Larson', born:1989})
            // initialize CREATE
            string cypher = $"CREATE (:{lable} {{";

            // add identifier as required property
            cypher += $"identifier:'{cypherIdentifier}', ";

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
                //kvp.Value.TrimEnd() // removes all whitespaces at the end of the string
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

    internal class SibBW_GES_BW : SibBw
    {
        public string identifier { get { return ("BWNR" + stringValues["BWNR"]).Replace(" ", "_"); } }
        
        public List<SibBW_TEIL_BW> teilbauwerke = new List<SibBW_TEIL_BW>();

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, SibBW_GES_BW_constAttributes.label);
        }
    }

    internal static class SibBW_GES_BW_constAttributes
    {
        public static string label = "GES_BW";
    }

    internal class SibBW_TEIL_BW : SibBw
    {
        public string identifier { get { return ("ID_NR" + stringValues["ID_NR"]).Replace(" ", "_"); } }

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, SibBW_TEIL_BW_constAttributes.label);
        }
    }

    internal static class SibBW_TEIL_BW_constAttributes
    {
        public static string label = "TEIL_BW";
    }

    internal class SibBW_PRUFALT : SibBw
    {
        public string identifier { get { return ("ID_NR" + stringValues["ID_NR"] + 
                    "_" + numberValues["PRUFJAHR"] + "_" + stringValues["PRUFART"]).Replace(" ", "_"); } }

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, SibBW_PRUFALT_constAttributes.label);
        }
    }

    internal static class SibBW_PRUFALT_constAttributes
    {
        public static string label = "PRUFALT";
    }

    internal class SibBW_SCHADFALT : SibBw
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

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, SibBW_SCHADALT_constAttributes.label, KeyValuePair.Create("identifierPruf", identifierPruf));
        }
    }

    internal static class SibBW_SCHADALT_constAttributes
    {
        public static string label = "SCHADALT";
    }
}
