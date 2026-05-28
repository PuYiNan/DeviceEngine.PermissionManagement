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

        [TestInitialize]
        public void TestInitialize()
        {
            _testConfigPath = Path.GetTempFileName() + ".json";
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
                Roles =
                {
                    new Role
                    {
                        Name = "Admin",
                        Permissions =
                        {
                            new Permission
                            {
                                Name = "FullAccess",
                                DisabledControls = { },
                                HiddenControls = { }
                            }
                        }
                    },
                    new Role
                    {
                        Name = "Operator",
                        Permissions =
                        {
                            new Permission
                            {
                                Name = "BasicAccess",
                                DisabledControls = { "btnDelete" },
                                HiddenControls = { }
                            }
                        }
                    },
                    new Role
                    {
                        Name = "ReadOnly",
                        Permissions =
                        {
                            new Permission
                            {
                                Name = "ReadOnlyAccess",
                                DisabledControls = { "btnSave", "btnDelete" },
                                HiddenControls = { "pnlAdmin" }
                            }
                        }
                    }
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_testConfigPath, json);
        }

        [TestMethod]
        public void LoadConfiguration_ValidFile_LoadsSuccessfully()
        {
            PermissionManager.Instance.LoadConfiguration(_testConfigPath);
            
            Assert.IsNotNull(PermissionManager.Instance.GetCurrentRole());
            Assert.AreEqual("Operator", PermissionManager.Instance.GetCurrentRole().Name);
        }

        [TestMethod]
        public void SetCurrentRole_ValidRole_ChangesRole()
        {
            PermissionManager.Instance.LoadConfiguration(_testConfigPath);
            PermissionManager.Instance.SetCurrentRole("Admin");
            
            Assert.AreEqual("Admin", PermissionManager.Instance.GetCurrentRole().Name);
        }

        [TestMethod]
        public void CheckControlEnabled_ControlInDisabledList_ReturnsFalse()
        {
            PermissionManager.Instance.LoadConfiguration(_testConfigPath);
            PermissionManager.Instance.SetCurrentRole("Operator");
            
            bool result = PermissionManager.Instance.CheckControlEnabled("btnDelete");
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckControlEnabled_ControlNotInDisabledList_ReturnsTrue()
        {
            PermissionManager.Instance.LoadConfiguration(_testConfigPath);
            PermissionManager.Instance.SetCurrentRole("Operator");
            
            bool result = PermissionManager.Instance.CheckControlEnabled("btnSave");
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CheckControlVisible_ControlInHiddenList_ReturnsFalse()
        {
            PermissionManager.Instance.LoadConfiguration(_testConfigPath);
            PermissionManager.Instance.SetCurrentRole("ReadOnly");
            
            bool result = PermissionManager.Instance.CheckControlVisible("pnlAdmin");
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckControlVisible_ControlNotInHiddenList_ReturnsTrue()
        {
            PermissionManager.Instance.LoadConfiguration(_testConfigPath);
            PermissionManager.Instance.SetCurrentRole("ReadOnly");
            
            bool result = PermissionManager.Instance.CheckControlVisible("btnSave");
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CheckControlEnabled_AdminRole_AllControlsEnabled()
        {
            PermissionManager.Instance.LoadConfiguration(_testConfigPath);
            PermissionManager.Instance.SetCurrentRole("Admin");
            
            bool result = PermissionManager.Instance.CheckControlEnabled("btnDelete");
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void PermissionMerge_MultiplePermissions_MergesDisabledControls()
        {
            var config = new PermissionConfig
            {
                CurrentRole = "TestRole",
                Roles =
                {
                    new Role
                    {
                        Name = "TestRole",
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
                        }
                    }
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_testConfigPath, json);

            PermissionManager.Instance.LoadConfiguration(_testConfigPath);

            Assert.IsFalse(PermissionManager.Instance.CheckControlEnabled("btnA"));
            Assert.IsFalse(PermissionManager.Instance.CheckControlEnabled("btnB"));
            Assert.IsFalse(PermissionManager.Instance.CheckControlEnabled("btnC"));
        }
    }
}