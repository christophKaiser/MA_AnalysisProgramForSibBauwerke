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

        protected string GetCypherCreate(string cypherIdentifier, string lable)
        {
            // CREATE (a:Person {name:'Brie Larson', born:1989})
            // initialize CREATE
            string cypher = $"CREATE ({cypherIdentifier}:{lable} {{";

            // add identifier as required property
            cypher += $"identifier:'{cypherIdentifier}', ";

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
        public string identifier { get { return "BWNR" + stringValues["BWNR"]; } }
        public string label = "GES_BW";
        public List<SibBW_TEIL_BW> teilbauwerke = new List<SibBW_TEIL_BW>();

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, label);
        }

        public string GetCypherCreateMerge_BW_TeilBWs()
        {
            string cypher = GetCypherCreate();
            foreach (SibBW_TEIL_BW teilBW in teilbauwerke)
            {
                cypher += "\n" + teilBW.GetCypherCreate();
                cypher += "\n" + GetCypherMerge(identifier, teilBW.identifier, "BW_TeilBW");
            }
            return cypher;
        }
    }

    internal class SibBW_TEIL_BW : SibBw
    {
        public string identifier { get { return "ID_NR" + stringValues["ID_NR"].Replace(" ", "_"); } }
        public string label = "TEIL_BW";

        public string GetCypherCreate()
        {
            return GetCypherCreate(identifier, label);
        }
    }
}
