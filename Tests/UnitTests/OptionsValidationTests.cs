using System;
using NUnit.Framework;
using SqrlForNet;

namespace UnitTests
{
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
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(""));
        }

        [Test]
        public void Should_ThrowArgumentException_When_QrCodeScaleLessThan1()
        {
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(""));
        }

        [Test]
        public void Should_ThrowArgumentException_When_CallbackPathIsNull()
        {
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(""));
        }

        [Test]
        public void Should_ThrowArgumentException_When_CallbackPathIsEmpty()
        {
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(""));
        }

        [Test]
        public void Should_ThrowArgumentException_When_CallbackPathNotHaveABackslashAtTheStart()
        {
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(""));
        }

        [Test]
        public void Should_ThrowArgumentException_When_RedirectPathIsSetButDoesNotStartWithBackslash()
        {
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(""));
        }

        [Test]
        public void Should_ThrowArgumentException_When_OtherAuthenticationPathsHasADuplicatePath()
        {
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(""));
        }

        [Test]
        public void Should_ThrowArgumentException_When_OtherAuthenticationPathsEntryDoesNotHaveABackslashAtTheStart()
        {
            Assert.That(_classUnderTest.Validate, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(""));
        }
        
        
    }
}