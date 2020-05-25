using UnityEngine;

namespace E7.Firebase
{
    public static class Firestorm
    {
        internal const string assetMenuName = nameof(Firebase) + "/";
        internal const string restApiBaseUrl = "https://firestore.googleapis.com/v1beta1";

        /// <summary>
        /// Start building the collection-document path here.
        /// </summary>
        public static FirestormCollectionReference Collection(string name) => new FirestormCollectionReference(name);

        // TREEVIEW - just get and set this from REST Authentication, instead of using Firebase Authentication SDK
        public static string idToken;
        
        
    }
}