using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;

namespace CloudTrace.Api.Repositories;

public class FirestoreRepository
{
    private readonly FirestoreDb _db;
    private const string COLLECTION = "incidents";

    public FirestoreRepository(IConfiguration configuration)
    {
        var projectId = configuration["GCP_PROJECT_ID"] 
            ?? Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
            ?? throw new InvalidOperationException("GCP_PROJECT_ID is not configured.");
        
        _db = FirestoreDb.Create(projectId);
    }

    public async Task<List<Dictionary<string, object>>> GetIncidentsAsync(int limit = 50)
    {
        var collection = _db.Collection(COLLECTION);
        var query = collection
            .OrderByDescending("start_ts")
            .Limit(limit);
            
        var snapshot = await query.GetSnapshotAsync();
        
        return snapshot.Documents
            .Select(d => {
                var dict = d.ToDictionary();
                dict["id"] = d.Id; // Ensure ID is included
                return dict;
            })
            .ToList();
    }

    public async Task<Dictionary<string, object>?> GetIncidentByIdAsync(string id)
    {
        var docRef = _db.Collection(COLLECTION).Document(id);
        var snapshot = await docRef.GetSnapshotAsync();
        
        if (!snapshot.Exists) return null;
        
        var dict = snapshot.ToDictionary();
        dict["id"] = snapshot.Id;
        return dict;
    }
}
