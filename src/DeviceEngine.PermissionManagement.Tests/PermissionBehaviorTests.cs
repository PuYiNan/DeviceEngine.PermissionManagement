using DeviceEngine.PermissionManagement.Behaviors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Controls;

namespace DeviceEngine.PermissionManagement.Tests
{
    [TestClass]
    public class PermissionBehaviorTests
    {
        [TestMethod]
        public void PermissionTag_SetValue_GetValueReturnsSame()
        {
            var button = new Button();
            string expectedTag = "TestControl";

            PermissionBehavior.SetPermissionTag(button, expectedTag);
            string actualTag = PermissionBehavior.GetPermissionTag(button);

            Assert.AreEqual(expectedTag, actualTag);
        }

        [TestMethod]
        public void PermissionTag_DefaultValue_IsNull()
        {
            var button = new Button();

            string tag = PermissionBehavior.GetPermissionTag(button);

            Assert.IsNull(tag);
        }

        [TestMethod]
        public void AutoCheck_DefaultValue_IsTrue()
        {
            var button = new Button();

            bool autoCheck = PermissionBehavior.GetAutoCheck(button);

            Assert.IsTrue(autoCheck);
        }

        [TestMethod]
        public void AutoCheck_SetValue_GetValueReturnsSame()
        {
            var button = new Button();

            PermissionBehavior.SetAutoCheck(button, false);
            bool result = PermissionBehavior.GetAutoCheck(button);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckMode_DefaultValue_IsBoth()
        {
            var button = new Button();

            CheckMode mode = PermissionBehavior.GetCheckMode(button);

            Assert.AreEqual(CheckMode.Both, mode);
        }

        [TestMethod]
        public void CheckMode_SetValue_GetValueReturnsSame()
        {
            var button = new Button();

            PermissionBehavior.SetCheckMode(button, CheckMode.Enabled);
            CheckMode result = PermissionBehavior.GetCheckMode(button);

            Assert.AreEqual(CheckMode.Enabled, result);
        }
    }
}