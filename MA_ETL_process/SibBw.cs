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
        public Dictionary<string, double> numberValues = new Dictionary<string, double>();

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
            foreach (KeyValuePair<string, double> kvp in numberValues)
            {
                cypher += $"{kvp.Key}:{kvp.Value}, ";
            }

            // add stringValues as properties
            foreach (KeyValuePair<string, string> kvp in stringValues)
            {
                cypher += $"{kvp.Key}:'{kvp.Value.Replace("'", "\\'")}', ";
                //kvp.Value.TrimEnd() // removes all whitespaces at the end of the string
            }

            // remove last ", " in the cypher-string
            cypher = cypher.Remove(cypher.Length - 2, 1) ;

            // close properties-parenthes and CREATE-parenthesis; return string
            return cypher += "})";
        }

        protected string GetCypherMerge(string identifierBase, string identifierTarget, string label)
        {
            // MERGE (a)-[r:ACTED_IN]->(b) SET r.roles = ['Carol Danvers']
            return $"MERGE ({identifierBase})-[:{label}]->({identifierTarget})";
        }
    }

    internal class SibBW_GES_BW : SibBw
    {
        public string identifier { get { return ("BWNR" + stringValues["BWNR"]).Replace(" ", "_"); } }
        public string label = "GES_BW";
        public List<SibBW_TEIL_BW> teilbauwerke = new List<SibBW_TEIL_BW>();

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, label);
        }
    }

    internal class SibBW_TEIL_BW : SibBw
    {
        public string identifier { get { return ("ID_NR" + stringValues["ID_NR"]).Replace(" ", "_"); } }
        public string label = "TEIL_BW";

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, label);
        }
    }

    internal class SibBW_PRUFALT : SibBw
    {
        public string identifier { get { return ("ID_NR" + stringValues["ID_NR"] + 
                    "_" + stringValues["PRUFJAHR"] + "_" + stringValues["PRUFART"]).Replace(" ", "_"); } }
        public string label = "PRUFALT";

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, label);
        }
    }

    internal class SibBW_SCHADFALT : SibBw
    {
        public string identifier { get {
                // ID_NR, PRUFJAHR, PRA (=Prüfart: {E, H})
                return ("ID_NR" + stringValues["ID_NR"] + "_" + stringValues["PRUFJAHR"] 
                    + "_" + stringValues["PRA"] + "_" + stringValues["IDENT"]).Replace(" ", "_");
            }
        }
        public string identifierPruf
        {
            get
            {
                // ID_NR, PRUFJAHR, PRA (=Prüfart: {E, H})
                return ("ID_NR" + stringValues["ID_NR"].Replace(" ", "_") +
                    "_" + stringValues["PRUFJAHR"] + "_" + stringValues["PRA"]).Replace(" ", "_");
            }
        }
        public string label = "SCHADALT";

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, label, KeyValuePair.Create("identifierPruf", identifierPruf));
        }
    }
}
