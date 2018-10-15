using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage; // Namespace for Storage Client Library
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using System.Linq;
using System;
using System.Threading;
using System.Collections.Generic;

namespace FhirClient.Services
{

    public interface IPatientReservoir
    {
        Task<int> GetNumberOfPatientsAsync();
        Task<List<string>> GetPatients(int maxPatients = 0);
        Task<string> GetFhirBundleAndDelete(string patientName);
        Task<string> GetNextPatientFileName();
        Task<string> GetNextPatientJson();
    }
    public class AzureFilesPatientReservoir : IPatientReservoir
    {

        private CloudStorageAccount StorageAccount { get; set; }
        private CloudFileClient FileClient { get; set; }
        private CloudFileShare FileShare { get; set; }
        private CloudFileDirectory PatientDirectory { get; set; }
        private const string fhirFileFolder = "fhir";

        public AzureFilesPatientReservoir(string connectionString, string shareName)
        {
            StorageAccount = CloudStorageAccount.Parse(connectionString);
            FileClient = StorageAccount.CreateCloudFileClient();
            FileShare = FileClient.GetShareReference(shareName);
            PatientDirectory = FileShare.GetRootDirectoryReference().GetDirectoryReference(fhirFileFolder);
        }

        public async Task<List<string>> GetPatients(int maxPatients = 0)
        {
            var ret = new List<string>();
            int numPatients = 0;
            FileContinuationToken token = null;
            try
            {
                do
                {
                    FileResultSegment resultSegment = await PatientDirectory.ListFilesAndDirectoriesSegmentedAsync(token);
                    foreach (var item in resultSegment.Results)
                    {
                        string filename = System.IO.Path.GetFileName(item.Uri.LocalPath);
                        if (item.Uri.Segments.Last().Contains(".json") && !item.Uri.Segments.Last().Contains("hospitalInformation"))
                        {
                            ret.Add(item.Uri.Segments.Last());
                            numPatients++;
                        }
                        if (maxPatients != 0 && numPatients >= maxPatients)
                        {
                            return ret;
                        }
                    }
                    token = resultSegment.ContinuationToken;
                }
                while (token != null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return ret;
        }

        public async Task<int> GetNumberOfPatientsAsync()
        {
            return (await GetPatients()).Count;
        }

        public async Task<string> GetNextPatientFileName()
        {
            var patList = await GetPatients(1);
            if (patList.Count > 0) {
                return patList.First();
            }
            return "";
        }

        public async Task<string> GetFhirBundleAndDelete(string fileName)
        {
            string ret = "";
            CloudFile source = PatientDirectory.GetFileReference(fileName);
            if (await source.ExistsAsync())
            {
                ret = await source.DownloadTextAsync();
                await source.DeleteAsync();
            }
            return ret;
        }

        public async Task<string> GetNextPatientJson()
        {
            string patientName = await GetNextPatientFileName();
            return await GetFhirBundleAndDelete(patientName);
        }   
    }

}