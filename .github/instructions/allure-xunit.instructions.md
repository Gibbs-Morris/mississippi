---
applyTo: '**/*.cs'
---

# Allure xUnit Test Naming and Organization

Governing thought: Standardize Allure.Xunit attribute usage for xUnit tests to produce stable, domain-aligned test hierarchies that map vertical architecture through Suite taxonomy and horizontal backlog through Epic/Feature/Story semantics.

## Rules (RFC 2119)

- Test methods **MUST** use `MethodName_Should_Outcome_GivenCondition` naming per testing.instructions.md.  
  Why: Maintains consistency with repository-wide test naming standards.
- Each test **MUST** declare exactly one `[AllureParentSuite]`, `[AllureSuite]`, `[AllureSubSuite]`, and `[AllureEpic]`.  
  Why: Ensures consistent hierarchical organization and complete vertical/horizontal classification.
- `[AllureFeature]` and `[AllureStory]` **SHOULD** be single-valued by default; multiple values **MAY** be used only when a test legitimately spans features/stories with documented justification.  
  Why: Keeps tests focused and avoids ambiguous multi-dimensional classification.
- Suite names (ParentSuite/Suite/SubSuite) **MUST** represent vertical architecture slices (bounded contexts, modules, components) in Title Case using domain language.  
  Why: Maps test organization to system structure for architectural traceability.
- Epic/Feature/Story names **MUST** represent horizontal backlog semantics (business capabilities, user journeys, acceptance criteria) in Title Case using domain language.  
  Why: Aligns test reporting with product backlog for stakeholder communication.
- All Allure attribute values **MUST** be stable identifiers that **MUST NOT** include timestamps, GUIDs, environment names, customer-specific data, or deployment-specific context.  
  Why: Ensures test identity remains constant across runs and environments for reliable trend analysis.
- Attribute values **MUST** use Title Case and domain vocabulary; attribute values **MUST NOT** use technical type names, method names, or implementation details.  
  Why: Makes test reports accessible to non-technical stakeholders and aligns with ubiquitous language.
- Tests **MUST** use either xUnit `[Fact(DisplayName = "...")]`/`[Theory(DisplayName = "...")]` OR `[AllureName("...")]`; tests **MUST NOT** set both unless documented that DisplayName takes precedence in this repository.  
  Why: Avoids conflicting test names in different reporting contexts.
- `[AllureTag]` values **MUST** follow "key:value" format (e.g., `"layer:unit"`, `"risk:high"`); tags **MUST NOT** duplicate hierarchy information already in Suite/Epic attributes.  
  Why: Enables orthogonal classification without redundancy.
- `[AllureOwner]` **MUST** specify a team name or alias, **MUST NOT** specify individual person names.  
  Why: Ensures ownership survives personnel changes and supports team-based accountability.
- `[AllureId]` **MUST** be unique within the test suite and stable over time; **MUST NOT** change when test implementation refactors.  
  Why: Preserves test history and traceability across refactoring.
- Step names in `[AllureStep]` **MUST** be verb phrases in domain language (e.g., "Submit order with valid payment"); step parameters **SHOULD** use human-friendly names; sensitive parameters **MUST** be skipped or masked.  
  Why: Makes step logs readable for business analysis while protecting sensitive data.
- Tests spanning multiple bounded contexts **MUST** be split into separate tests with one vertical slice (ParentSuite/Suite/SubSuite) per test.  
  Why: Maintains clean architectural boundaries and avoids cross-concern coupling in test organization.
- Allure attributes **SHOULD** be placed immediately before the test method, after XML documentation comments.  
  Why: Follows C# attribute placement conventions and keeps test intent visible.
- Developers **SHOULD** reference testing.instructions.md for test structure, L0-L4 levels, and xUnit patterns; this file covers only Allure-specific taxonomy.  
  Why: Avoids duplication and maintains single source of truth for general testing standards.

## Scope and Audience

**Audience:** Developers writing xUnit tests with Allure.Xunit annotations in Mississippi framework and samples.

**In scope:** Allure attribute naming, hierarchy mapping, taxonomy rules, stability requirements, cross-references to testing.instructions.md.

**Out of scope:** General xUnit patterns (see testing.instructions.md), test implementation details, Allure server configuration.

## Purpose

This document establishes Allure.Xunit attribute standards ensuring test reports map cleanly to system architecture (vertical) and product backlog (horizontal) using stable, domain-aligned naming that supports long-term traceability and stakeholder communication.

## At-a-Glance Quick-Start

- Use ParentSuite/Suite/SubSuite for vertical architecture (bounded contexts → modules → components)
- Use Epic/Feature/Story for horizontal backlog (capabilities → user journeys → acceptance criteria)
- All names in Title Case, domain language, stable across runs
- One vertical slice per test; split cross-context tests
- Tags in "key:value" format for orthogonal classification

> **Drift check:** For general test execution and organization, reference `eng/src/agent-scripts/unit-test-*.ps1` scripts and testing.instructions.md. This file covers only Allure taxonomy rules.

## Core Principles and Rationale

- **Dual-axis classification:** Suite-based (vertical architecture) + Behavior-based (horizontal backlog) enables both technical and business views of test coverage.
- **Stable naming:** Test identity must survive refactoring, environment changes, and team transitions to preserve historical trends and traceability.
- **Domain language:** Title Case business terms make reports accessible to product owners, business analysts, and stakeholders who don't read code.
- **Single responsibility per test:** One vertical slice and one primary epic keep tests focused and avoid ambiguous multi-dimensional ownership.

## Taxonomy Mapping

### Vertical Architecture (Suite Hierarchy)

Maps system structure from coarse to fine:

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[AllureParentSuite]` | Bounded context or major subsystem | "Order Management", "Payment Processing" |
| `[AllureSuite]` | Module or service within context | "Invoice Generation", "Refund Handling" |
| `[AllureSubSuite]` | Component or specific functionality | "PDF Export", "Email Notifications" |

### Horizontal Backlog (Behavior Hierarchy)

Maps product backlog from strategic to tactical:

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[AllureEpic]` | High-level business capability or theme | "Customer Self-Service", "Compliance Reporting" |
| `[AllureFeature]` | User-facing feature or journey | "Order Tracking", "Tax Calculation" |
| `[AllureStory]` | Acceptance criterion or user story | "View Order Status", "Calculate Sales Tax for EU" |

### Orthogonal Classification (Tags, Owner, ID)

| Attribute | Purpose | Format | Example |
|-----------|---------|--------|---------|
| `[AllureTag]` | Cross-cutting concerns | "key:value" | `"layer:unit"`, `"risk:critical"` |
| `[AllureOwner]` | Responsible team/alias | Team name | "Platform Team", "Payments Squad" |
| `[AllureId]` | Unique stable identifier | Alphanumeric string | "ORD-001", "PAY-INV-042" |

## Procedures

### Assigning Suite Hierarchy (Vertical)

1. Identify the bounded context or major subsystem for the code under test → `[AllureParentSuite]`
2. Identify the module or service within that context → `[AllureSuite]`
3. Identify the component or specific class/feature → `[AllureSubSuite]`

**Why:** Creates architectural traceability from high-level system structure down to implementation details.

### Assigning Behavior Hierarchy (Horizontal)

1. Identify the business capability or strategic theme → `[AllureEpic]`
2. Identify the user-facing feature or journey (optional but recommended) → `[AllureFeature]`
3. Identify the acceptance criterion or user story (optional) → `[AllureStory]`

**Why:** Aligns test reporting with product roadmap and backlog for stakeholder communication.

### Handling Cross-Context Tests

1. If a test validates behavior spanning multiple bounded contexts, split into separate tests.
2. Each test declares one ParentSuite/Suite/SubSuite representing its primary vertical slice.
3. Use `[AllureLink]` to cross-reference related tests if needed.

**Why:** Maintains clean architectural boundaries and prevents ambiguous test ownership.

## Examples

### Example 1: L0 Unit Test with Full Allure Taxonomy

```csharp
namespace Mississippi.Orders.Invoicing.Tests;

/// <summary>
/// Tests for invoice PDF generation.
/// </summary>
public sealed class InvoicePdfGeneratorTests
{
    /// <summary>
    /// Validates PDF generation for standard invoice.
    /// </summary>
    [AllureParentSuite("Order Management")]
    [AllureSuite("Invoice Generation")]
    [AllureSubSuite("PDF Export")]
    [AllureEpic("Customer Self-Service")]
    [AllureFeature("Invoice Download")]
    [AllureStory("Generate PDF Invoice")]
    [AllureTag("layer:unit")]
    [AllureTag("risk:medium")]
    [AllureOwner("Platform Team")]
    [AllureId("INV-PDF-001")]
    [Fact]
    public void GeneratePdf_Should_CreateValidDocument_GivenStandardInvoice()
    {
        // Arrange
        var invoice = new Invoice { Id = "INV-123", Amount = 100.00m };
        var generator = new InvoicePdfGenerator();

        // Act
        var pdf = generator.Generate(invoice);

        // Assert
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
    }
}
```

### Example 2: Theory with AllureStep and Parameter Masking

```csharp
namespace Mississippi.Payments.Refunds.Tests;

/// <summary>
/// Tests for refund processing with sensitive data.
/// </summary>
public sealed class RefundProcessorTests
{
    /// <summary>
    /// Validates refund calculation for various amounts.
    /// </summary>
    [AllureParentSuite("Payment Processing")]
    [AllureSuite("Refund Handling")]
    [AllureSubSuite("Calculation Engine")]
    [AllureEpic("Compliance Reporting")]
    [AllureFeature("Refund Processing")]
    [AllureOwner("Payments Squad")]
    [AllureId("REFUND-CALC-001")]
    [Theory]
    [InlineData(100.00, 10.00, 90.00)]
    [InlineData(50.00, 5.00, 45.00)]
    public void CalculateRefund_Should_ApplyFee_GivenPartialRefund(
        decimal originalAmount, 
        decimal fee, 
        decimal expectedRefund)
    {
        // Arrange
        var processor = new RefundProcessor();

        // Act
        ProcessRefundWithLogging(originalAmount, fee, out var actualRefund);

        // Assert
        Assert.Equal(expectedRefund, actualRefund);
    }

    [AllureStep("Process refund of {originalAmount:C} with fee {fee:C}")]
    private void ProcessRefundWithLogging(
        decimal originalAmount, 
        decimal fee, 
        out decimal refundAmount)
    {
        var processor = new RefundProcessor();
        refundAmount = processor.Calculate(originalAmount, fee);
    }
}
```

### Example 3: DisplayName vs AllureName (Use One)

```csharp
// ✅ GOOD: Use xUnit DisplayName
[AllureParentSuite("Order Management")]
[AllureSuite("Order Processing")]
[AllureSubSuite("Validation")]
[AllureEpic("Order Fulfillment")]
[AllureOwner("Platform Team")]
[AllureId("ORD-VAL-001")]
[Fact(DisplayName = "Validate order rejects negative quantities")]
public void ValidateOrder_Should_RejectOrder_GivenNegativeQuantity()
{
    // Test implementation
}

// ✅ ALSO GOOD: Use AllureName
[AllureParentSuite("Order Management")]
[AllureSuite("Order Processing")]
[AllureSubSuite("Validation")]
[AllureEpic("Order Fulfillment")]
[AllureOwner("Platform Team")]
[AllureId("ORD-VAL-002")]
[AllureName("Validate order rejects invalid customer ID")]
[Fact]
public void ValidateOrder_Should_RejectOrder_GivenInvalidCustomerId()
{
    // Test implementation
}

// ❌ BAD: Setting both creates ambiguity
[AllureName("Some name")]
[Fact(DisplayName = "Different name")]
public void Test_Creates_Ambiguous_Name()
{
    // Unclear which name wins in reports
}
```

## Anti-Patterns

- ❌ Using technical type names → ✅ Use domain language ("Invoice PDF Export" not "PdfGenerator class")
- ❌ Including timestamps in names → ✅ Use stable names ("Order Processing" not "Order Processing 2024-01-15")
- ❌ Person names in Owner → ✅ Use team/alias ("Platform Team" not "John Smith")
- ❌ Duplicating hierarchy in tags → ✅ Use orthogonal tags (`"risk:high"` not `"suite:invoice"`)
- ❌ Cross-context tests in single test → ✅ Split into separate tests per bounded context
- ❌ Method names as suite names → ✅ Use business capability names ("Tax Calculation" not "CalculateTax method")
- ❌ Setting both DisplayName and AllureName → ✅ Choose one naming approach per test
- ❌ Unstable AllureId (changing with refactor) → ✅ Keep ID stable across implementation changes

## Enforcement

- Code reviews **SHOULD** verify Allure attributes follow taxonomy rules and use stable, domain-aligned names.
- Teams **SHOULD** establish controlled vocabularies for ParentSuite/Suite/Epic values to maintain consistency.
- CI/test reports **SHOULD** be monitored for attribute completeness (all tests have required attributes).
- Teams **MAY** create custom analyzers to validate Allure attribute presence and format compliance.

## External References

- Allure.Xunit documentation: <https://github.com/allure-framework/allure-csharp>
- Testing strategy and L0-L4 levels: ./testing.instructions.md
- Test naming conventions: ./naming.instructions.md
- xUnit fundamentals: <https://xunit.net/>

---
Last verified: 2025-12-16  
Default branch: main
