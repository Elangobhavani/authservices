﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Kentor.AuthServices.TestHelpers;
using Kentor.AuthServices.Configuration;
using System.Configuration;

namespace Kentor.AuthServices.Tests
{
    [TestClass]
    public class IdentityProviderTests
    {
        [TestMethod]
        public void IdentityProvider_CreateAuthenticateRequest_Destination()
        {
            string idpUri = "http://idp.example.com/";
            
            var ip = new IdentityProvider(new Uri(idpUri));

            var r = ip.CreateAuthenticateRequest(null);

            r.ToXElement().Attribute("Destination").Should().NotBeNull()
                .And.Subject.Value.Should().Be(idpUri);
        }

        [TestMethod]
        public void IdentityProvider_CreateAuthenticateRequest_AssertionConsumerServiceUrlFromConfig()
        {
            var idp = IdentityProvider.ConfiguredIdentityProviders.First().Value;

            var r = idp.CreateAuthenticateRequest(null);

            r.AssertionConsumerServiceUrl.Should().Be(new Uri("http://localhost/Saml2AuthenticationModule/acs"));
        }

        [TestMethod]
        public void IdentityProvider_CreateAuthenticateRequest_IssuerFromConfig()
        {
            var idp = IdentityProvider.ConfiguredIdentityProviders.First().Value;

            var r = idp.CreateAuthenticateRequest(null);

            r.Issuer.Should().Be("https://github.com/KentorIT/authservices");
        }

        [TestMethod]
        public void IdentityProvider_Certificate_FromFile()
        {
            var idp = IdentityProvider.ConfiguredIdentityProviders.First().Value;

            idp.Certificate.ShouldBeEquivalentTo(SignedXmlHelper.TestCert);
        }

        [TestMethod]
        public void IdentityProvider_ConfigFromMetadata()
        {
            var entityId = "http://localhost:13428/idpmetadata";
            var idpFromMetadata = IdentityProvider.ConfiguredIdentityProviders[entityId];

            idpFromMetadata.Binding.Should().Be(Saml2BindingType.HttpPost);
            idpFromMetadata.Certificate.Thumbprint.Should().Be(SignedXmlHelper.TestCert.Thumbprint);
            idpFromMetadata.DestinationUri.Should().Be(new Uri("http://localhost:13428/acs"));
            idpFromMetadata.EntityId.Should().Be(entityId);
        }

        private IdentityProviderElement CreateConfig()
        {
            var config = new IdentityProviderElement();
            config.AllowConfigEdit(true);
            config.Binding = Saml2BindingType.HttpPost;
            config.SigningCertificate = new CertificateElement();
            config.SigningCertificate.AllowConfigEdit(true);
            config.SigningCertificate.FileName = "Kentor.AuthServices.Tests.pfx";
            config.DestinationUri = new Uri("http://idp.example.com/acs");
            config.EntityId = "http://idp.example.com";

            return config;
        }

        private static void TestMissingConfig(IdentityProviderElement config, string missingElement)
        {
            Action a = () => new IdentityProvider(config);

            string expectgedMessage = "Missing " + missingElement + " configuration on Idp " + config.EntityId + ".";
            a.ShouldThrow<ConfigurationException>(expectgedMessage);
        }

        [TestMethod]
        public void IdentityProvider_MissingBindingThrows()
        {
            var config = CreateConfig();
            config.Binding = 0;
            TestMissingConfig(config, "binding");
        }

        [TestMethod]
        public void IdentityProvider_MissingCertificateThrows()
        {
            var config = CreateConfig();
            config.SigningCertificate = null;
            TestMissingConfig(config, "signing certificate");
        }

        [TestMethod]
        public void IdentityProvider_MissingDestinationUriThrows()
        {
            var config = CreateConfig();
            config.DestinationUri = null;
            TestMissingConfig(config, "destination Uri");
        }
    }
}
