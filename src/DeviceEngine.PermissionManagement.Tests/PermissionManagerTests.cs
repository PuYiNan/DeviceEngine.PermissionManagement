using DeviceEngine.PermissionManagement.Managers;
using DeviceEngine.PermissionManagement.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DeviceEngine.PermissionManagement.Tests
{
    [TestClass]
    public class PermissionManagerTests
    {
        private string _testConfigPath;
        private PermissionManager _permissionManager;

        [TestInitialize]
        public void TestInitialize()
        {
            _testConfigPath = Path.GetTempFileName() + ".json";
            _permissionManager = new PermissionManager();
            CreateTestConfig();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }

        private void CreateTestConfig()
        {
            var config = new PermissionConfig
            {
                ScanMode = ScanMode.Hybrid,
                CurrentRole = "Operator",
                Permissions =
                {
                    new Permission
                    {
                        Name = "FullAccess",
                        DisabledControls = { },
                        HiddenControls = { }
                    },
                    new Permission
                    {
                        Name = "BasicAccess",
                        DisabledControls = { "btnDelete" },
                        HiddenControls = { }
                    },
                    new Permission
                    {
                        Name = "ReadOnlyAccess",
                        DisabledControls = { "btnSave", "btnDelete" },
                        HiddenControls = { "pnlAdmin" }
                    }
                },
                Roles =
                {
                    new Role
                    {
                        Name = "Admin",
                        PermissionNames = { "FullAccess" }
                    },
                    new Role
                    {
                        Name = "Operator",
                        PermissionNames = { "BasicAccess" }
                    },
                    new Role
                    {
                        Name = "ReadOnly",
                        PermissionNames = { "ReadOnlyAccess" }
                    }
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_testConfigPath, json);
        }

        [TestMethod]
        public void LoadConfiguration_ValidFile_LoadsSuccessfully()
        {
            _permissionManager.Initialize(_testConfigPath);

            Assert.IsNotNull(_permissionManager.GetCurrentRole());
            Assert.AreEqual("Operator", _permissionManager.GetCurrentRole().Name);
        }

        [TestMethod]
        public void SetCurrentRole_ValidRole_ChangesRole()
        {
            _permissionManager.Initialize(_testConfigPath);
            _permissionManager.SetCurrentRole("Admin");

            Assert.AreEqual("Admin", _permissionManager.GetCurrentRole().Name);
        }

        [TestMethod]
        public void CheckControlEnabled_ControlInDisabledList_ReturnsFalse()
        {
            _permissionManager.Initialize(_testConfigPath);
            _permissionManager.SetCurrentRole("Operator");

            bool result = _permissionManager.CheckControlEnabled("btnDelete");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckControlEnabled_ControlNotInDisabledList_ReturnsTrue()
        {
            _permissionManager.Initialize(_testConfigPath);
            _permissionManager.SetCurrentRole("Operator");

            bool result = _permissionManager.CheckControlEnabled("btnSave");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CheckControlVisible_ControlInHiddenList_ReturnsFalse()
        {
            _permissionManager.Initialize(_testConfigPath);
            _permissionManager.SetCurrentRole("ReadOnly");

            bool result = _permissionManager.CheckControlVisible("pnlAdmin");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckControlVisible_ControlNotInHiddenList_ReturnsTrue()
        {
            _permissionManager.Initialize(_testConfigPath);
            _permissionManager.SetCurrentRole("ReadOnly");

            bool result = _permissionManager.CheckControlVisible("btnSave");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CheckControlEnabled_AdminRole_AllControlsEnabled()
        {
            _permissionManager.Initialize(_testConfigPath);
            _permissionManager.SetCurrentRole("Admin");

            bool result = _permissionManager.CheckControlEnabled("btnDelete");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void PermissionMerge_MultiplePermissions_MergesDisabledControls()
        {
            var config = new PermissionConfig
            {
                CurrentRole = "TestRole",
                Permissions =
                {
                    new Permission
                    {
                        Name = "Perm1",
                        DisabledControls = { "btnA", "btnB" }
                    },
                    new Permission
                    {
                        Name = "Perm2",
                        DisabledControls = { "btnB", "btnC" }
                    }
                },
                Roles =
                {
                    new Role
                    {
                        Name = "TestRole",
                        PermissionNames = { "Perm1", "Perm2" }
                    }
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_testConfigPath, json);

            _permissionManager.Initialize(_testConfigPath);

            Assert.IsFalse(_permissionManager.CheckControlEnabled("btnA"));
            Assert.IsFalse(_permissionManager.CheckControlEnabled("btnB"));
            Assert.IsFalse(_permissionManager.CheckControlEnabled("btnC"));
        }
    }
}
