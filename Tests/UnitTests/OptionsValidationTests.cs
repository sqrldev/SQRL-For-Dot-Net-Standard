using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using SqrlForNet;

namespace UnitTests
{
    [ExcludeFromCodeCoverage]
    public class OptionsValidationTests
    {

        private SqrlAuthenticationOptions _classUnderTest;
        
        [SetUp]
        public void Setup()
        {
            _classUnderTest = new SqrlAuthenticationOptions();
        }

        [Test]
        public void Should_ThrowArgumentException_When_EncryptionKeyIsNull()
        {
            _classUnderTest.EncryptionKey = null;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("EncryptionKey must be set with some bytes or don't override it and secure random bytes are generated."));
        }
        
        [Test]
        public void Should_ThrowArgumentException_When_EncryptionKeyIsEmpty()
        {
            _classUnderTest.EncryptionKey = new byte[0];
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("EncryptionKey must be set with some bytes or don't override it and secure random bytes are generated."));
        }
        
        [Test]
        public void Should_ThrowArgumentException_When_NutExpiresInSecondsIsLessThan1()
        {
            _classUnderTest.NutExpiresInSeconds = 0;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("NutExpiresInSeconds must be grater than 0 so that a SQRL client can have a chance to communicate, we suggest a value of 60"));
        }

        [Test]
        public void Should_ThrowArgumentException_When_QrCodeBorderSizeLessThan1()
        {
            _classUnderTest.QrCodeBorderSize = 0;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("QrCodeBorderSize must be 1 or higher."));
        }

        [Test]
        public void Should_ThrowArgumentException_When_QrCodeScaleLessThan1()
        {
            _classUnderTest.QrCodeScale = 0;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("QrCodeScale must be 1 or higher."));
        }

        [Test]
        public void Should_ThrowArgumentException_When_CallbackPathIsNull()
        {
            _classUnderTest.CallbackPath = null;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("The CallbackPath should have a value"));
        }

        [Test]
        public void Should_ThrowArgumentException_When_CallbackPathIsEmpty()
        {
            _classUnderTest.CallbackPath = string.Empty;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("The CallbackPath should have a value"));
        }

        [Test]
        public void Should_ThrowArgumentException_When_OtherAuthenticationPathIsNull()
        {
            _classUnderTest.OtherAuthenticationPaths = new[]
            {
                new OtherAuthenticationPath()
                {
                    Path = null
                }
            };
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("One of the OtherAuthenticationPath needs a path defining as it currently doesn't have one"));
        }
        
        [Test]
        public void Should_ThrowArgumentException_When_OtherAuthenticationPathsHasADuplicatePath()
        {
            _classUnderTest.OtherAuthenticationPaths = new[]
            {
                new OtherAuthenticationPath()
                {
                    Path = "/DuplicatePath"
                },
                new OtherAuthenticationPath()
                {
                    Path = "/DuplicatePath"
                }
            };
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("/DuplicatePath is entered more than once in OtherAuthenticationPaths\r\n/DuplicatePath is entered more than once in OtherAuthenticationPaths\r\n"));
        }

        [Test]
        public void Should_ThrowArgumentException_When_HelpersIsEnabledButHelpersPathsIsNull()
        {
            _classUnderTest.EnableHelpers = true;
            _classUnderTest.HelpersPaths = null;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("HelpersPaths must have at least one path when EnableHelpers is true."));
        }

        [Test]
        public void Should_ThrowArgumentException_When_HelpersIsEnabledButDuplicateHelpPathDefined()
        {
            _classUnderTest.EnableHelpers = true;
            _classUnderTest.HelpersPaths = new[]
            {
                new PathString("/unit/test"),
                new PathString("/unit/test")
            };
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("/unit/test is entered more than once in HelpersPaths"));
        }

        [Test]
        public void Should_ThrowArgumentException_When_BothUsersExistsAreNull()
        {
            _classUnderTest.UserExists = null;
            _classUnderTest.UserExistsAsync = null;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("UserExists should be set so that you can validate users"));
        }

        [Test]
        public void Should_ThrowArgumentException_When_BothUsersExistsAreNotNull()
        {
            _classUnderTest.UserExists = (s, context) => { return UserLookUpResult.Unknown; };
            _classUnderTest.UserExistsAsync = (s, context) => { return Task.FromResult(UserLookUpResult.Unknown); }; ;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("UserExists and UserExistsAsync are both defined you should only define one of them."));
        }

        [Test]
        public void Should_ThrowArgumentException_When_BothUpdateUserIdAreNull()
        {
            _classUnderTest.UserExists = (s, context) => { return UserLookUpResult.Unknown; };
            _classUnderTest.UpdateUserId = null;
            _classUnderTest.UpdateUserIdAsync = null;
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("UpdateUserId should be set so that you can update your user id for a SQRL user"));
        }

        [Test]
        public void Should_ThrowArgumentException_When_BothUpdateUserIdAreNotNull()
        {
            _classUnderTest.UserExists = (s, context) => { return UserLookUpResult.Unknown; };
            _classUnderTest.UpdateUserId = (s, s1, arg3, arg4, arg5) => {};
            _classUnderTest.UpdateUserIdAsync = (s, s1, arg3, arg4, arg5) => { return Task.FromResult(0); };
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("UpdateUserId and UpdateUserIdAsync are both defined you should only define one of them."));
        }

        [Test]
        public void Should_ThrowArgumentException_When_BothCreateUserAreNotNull()
        {
            _classUnderTest.UserExists = (s, context) => { return UserLookUpResult.Unknown; };
            _classUnderTest.UpdateUserId = (s, s1, arg3, arg4, arg5) => { };
            _classUnderTest.CreateUser = (s, s1, arg3, arg4) => {  };
            _classUnderTest.CreateUserAsync = (s, s1, arg3, arg4) => { return Task.FromResult(0); };
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("UpdateUserId and UpdateUserIdAsync are both defined you should only define one of them."));
        }

    }
}