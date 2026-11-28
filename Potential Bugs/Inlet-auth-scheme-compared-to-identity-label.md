# Projection authorization can reject a user authenticated by the requested handler

## Source location

- Project: `Inlet.Gateway`.
- Source file: [src/Inlet.Gateway/InletHub.cs lines 89-95](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway/InletHub.cs#L89-L95).
- Type: `InletHub`.
- Member: `HasMatchingAuthenticationScheme / AuthorizeWithPolicyAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The hub compares an authentication handler's scheme name with ClaimsIdentity.AuthenticationType, the label on the resulting identity. These are separate settings. A standard Bearer JWT handler can create an identity labelled Federation rather than Bearer, so the comparison rejects it before checking its authorization requirements.

## Trigger

Use a valid JWT authenticated by a handler registered as Bearer and a projection/default policy selecting Bearer, retaining the token-validation default AuthenticationType of AuthenticationTypes.Federation. All policy claims/roles otherwise pass.

## Potential impact

A user who can pass the HTTP controller policy can be denied the live projection subscription with the same credentials. This is a false denial; the report does not claim an authorization bypass.

## Evidence

- [src/Inlet.Gateway/InletHub.cs lines 79-96](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway/InletHub.cs#L79-L96), [src/Inlet.Gateway/InletHub.cs lines 216-226](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway/InletHub.cs#L216-L226): A failed identity-label comparison throws SubscriptionDenied before evaluating policy requirements.
- [src/Inlet.Gateway/InletHub.cs lines 183-189](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway/InletHub.cs#L183-L189), [src/Inlet.Gateway/InletHub.cs lines 257-264](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway/InletHub.cs#L257-L264): Projection metadata/policy handler names are copied into AuthorizationPolicy.AuthenticationSchemes.
- [tests/Inlet.Gateway.L0Tests/InletHubAuthorizationTests.cs lines 30-31](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/Inlet.Gateway.L0Tests/InletHubAuthorizationTests.cs#L30-L31), [tests/Inlet.Gateway.L0Tests/InletHubAuthorizationTests.cs lines 160-184](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/Inlet.Gateway.L0Tests/InletHubAuthorizationTests.cs#L160-L184): Existing scheme denial coverage uses a synthetic TestAuthType identity and does not prove real JWT handler-name/identity-type equality.

- [Supporting reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters.defaultauthenticationtype?view=msal-web-dotnet-latest): Documents the default token-generated ClaimsIdentity authentication type as AuthenticationTypes.Federation, independently configurable from handler registration.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Authenticate a real token through the configured Bearer handler, capture the principal identity AuthenticationType, and compare controller policy success with hub subscribe denial.
- Do not claim an authorization bypass; this report is the concrete false-denial case. If application configuration deliberately makes identity type equal handler name, that deployment avoids the trigger.

## Confidence

**High**. The comparison equates distinct concepts; the default JWT identity label supplies a concrete common mismatch.
