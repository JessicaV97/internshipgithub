using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Happify.User
{
    /// <summary>
    /// Class to manage all users (list of the users, current user) and takes care of charging new lives.. 
    /// </summary>
    public class UserManager : MonoBehaviour
    {
        private const string UserDatabaseFilename = "Users4.json";

        [SerializeField]
        private float _maximumLives = 3;

        [SerializeField, Tooltip("The time it takes in seconds to receive a new life.")]
        private float _newLifeDuration = 10.0f; // in seconds

        private static UserManager _instance;
        private List<UserDescription> _allUsers = new List<UserDescription>();
        private UserDescription _currentUser;

        private float _difference;

        public float Difference => _difference; 
        public float NewLifeDuration => _newLifeDuration; 

        /// <summary>
        /// Gets all users.
        /// </summary>
        public IReadOnlyList<UserDescription> AllUsers => _allUsers;

        /// <summary>
        /// Gets the current user. Returns null if no user has been loaded.
        /// </summary>
        public UserDescription CurrentUser => _currentUser;

        /// <summary>
        /// Returns the instance of UserManager.
        /// </summary>
        public static UserManager Instance => _instance;

        /// <summary>
        /// An event that gets invoked when the current user's lives changes.
        /// </summary>
        public event Action CurrentUserLivesChanged;

        /// <summary>
        /// Create the UserManger instance ensure it does not get destroyed when changing scenes. 
        /// </summary>
        private void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(this);
                Load();
            }
            else
            {
                Destroy(this);
            }
        }

        /// <summary>
        /// If no user exists at the start, create a user called Jessica. Set to current user. 
        /// </summary>
        private void Start()
        {
            if(_allUsers.Any(user => user.Name == "Jessica"))
            {
                SetCurrentUser(_allUsers.FirstOrDefault(user => user.Name == "Jessica"));
            }
            else
            {
                AddUser(new UserDescription("Jessica", Level.Easy, Level.Easy, 3, true, true, 0, 0));
            }
        }

        /// <summary>
        /// Function to add a new user. 
        /// Probes the function to set the new user as current user. 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="setAsCurrentUser"></param>
        public void AddUser(UserDescription user, bool setAsCurrentUser = true)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            _allUsers.Add(user);

            if (setAsCurrentUser)
                SetCurrentUser(user);
        }

        /// <summary>
        /// Function to change the current user
        /// </summary>
        /// <param name="user"></param>
        public void SetCurrentUser(UserDescription user)
        {
            // Save current user if not null => writing to disk
            _currentUser = user;
        }

        /// <summary>
        /// Fuction to load all users. 
        /// </summary>
        private void Load()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, UserDatabaseFilename);
                if (!File.Exists(path))
                {
                    File.Create(path).Dispose();
                    Save();
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    _allUsers = new List<UserDescription>();
                else
                    _allUsers = JsonConvert.DeserializeObject<List<UserDescription>>(json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Store the list of users on a persistent datapath. 
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_allUsers);
                string path = Path.Combine(Application.persistentDataPath, UserDatabaseFilename);
                File.WriteAllText(path, json);
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Probe the function to update the number of lives for the current user. 
        /// </summary>
        private void Update()
        {
            UpdateCurrentUserLives();
        }

        /// <summary>
        /// Function that is used to charge a new life when it is lost. Until the maximum number of lives has been reached. 
        /// </summary>
        private void UpdateCurrentUserLives()
        {
            if (_currentUser == null || _currentUser.NrOfLives == _maximumLives)
                return;

            // Check if time has passed
            double timeNow = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            _difference = Convert.ToSingle(timeNow - _currentUser.LastLifeReceivedTimestamp); // added
            // Increment the number of lives
            if(timeNow - _currentUser.LastLifeReceivedTimestamp >= _newLifeDuration)
            {
                _currentUser.NrOfLives++;
                Save();
                _currentUser.LastLifeReceivedTimestamp = timeNow; //added
                CurrentUserLivesChanged?.Invoke();
            }
        }
    }
}