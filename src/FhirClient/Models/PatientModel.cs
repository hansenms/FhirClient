using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace FhirClient.Models
{
    public class PatientRecord
    {
        public Hl7.Fhir.Model.Patient Patient;
        public List<Hl7.Fhir.Model.Observation> Observations { get; set; }
        public List<Hl7.Fhir.Model.Encounter> Encounters { get; set; }
    }

}