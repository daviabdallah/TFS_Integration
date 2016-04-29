using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.VersionControl.Client ;
using Microsoft.TeamFoundation.VersionControl.Common;
using System.IO;


namespace TFS_Integration
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("TFS Integration Starting...");
            SortedDictionary<int, Changeset> changesets = GetChangeSets(new Uri("YOUR SOURCE SERVER URL"), "$/YOUR SOURCE SERVER PATH");

            foreach (KeyValuePair<int, Changeset> c in changesets)
            {
                Console.WriteLine(c.Key + "-" + c.Value.Comment);
                GetVersionByChangeset(new Uri("YOUR TARGET SERVER URL")
                    , "$/YOUR TARGET SERVER PATH"
                    , c.Key);
            }

        }

        static SortedDictionary<int,Changeset> GetChangeSets(Uri tfsUri, string serverPath)
        {
            TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(tfsUri);
            VersionControlServer vcs = tpc.GetService<VersionControlServer>();

            SortedDictionary<int, Changeset> Changesets = new SortedDictionary<int, Changeset>();


            foreach (Object obj in vcs.QueryHistory(serverPath,                       // path we care about ($/project/whatever) 
                                                    VersionSpec.Latest,            // version of that path
                                                    0,                             // deletion ID (0 = not deleted) 
                                                    RecursionType.Full,            // entire tree - full recursion
                                                    null,                          // include changesets from all users
                                                    new ChangesetVersionSpec(1),   // start at the beginning of time
                                                    VersionSpec.Latest,            // end at latest
                                                    200,                            // only return this many
                                                    false,                         // we don't want the files changed
                                                    true))                         // do history on the path
            {
                Changeset c = obj as Changeset;
                Changesets.Add(c.ChangesetId, c);
            }
            return Changesets;
        }

        static void GetVersionByChangeset(Uri tfsUri, string serverPath, int changesetId)
        {
            TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(tfsUri);
            VersionControlServer vcs = tpc.GetService<VersionControlServer>();
            Changeset changeset = vcs.GetChangeset(changesetId);

            WorkingFolder workfolder = new WorkingFolder(serverPath, @"C:\tsf_" + changesetId);
            Workspace workspace = vcs.CreateWorkspace(changesetId.ToString());
            workspace.CreateMapping(workfolder);
            GetStatus status = workspace.Get(VersionSpec.ParseSingleSpec(
                          String.Format("C{0}", changesetId), null)
                          , GetOptions.GetAll);
            workspace.Delete();
            StreamWriter fileWriter = new StreamWriter(@"C:\tsf_" + changesetId + @"\Database_snapshot.txt", false);
            fileWriter.WriteLine(changeset.Comment);
            fileWriter.Close();
            fileWriter.Dispose();

        }

    }
}
