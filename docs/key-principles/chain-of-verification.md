# Chain of Verification (CoVe)

Large language models hallucinate — they generate plausible but factually
incorrect outputs. The Chain of Verification (CoVe) process, published by Meta
AI Research in September 2023, provides a systematic method for an LLM to
self-correct by deliberately verifying its own draft responses before producing a
final answer.

**This document is written in Minto Pyramid format.**

---

## Governing Thought

CoVe is a four-step deliberation process — draft, plan verification questions,
answer those questions independently, then revise — that measurably reduces
hallucination in large language models across multiple task types.

---

## Situation

Large language models (LLMs) produce fluent, coherent text but frequently state
facts that are wrong. This phenomenon, termed **hallucination**, is one of the
most significant barriers to deploying LLMs in production systems where
correctness matters. Users cannot trust model outputs without external
verification, which defeats much of the productivity benefit.

## Complication

Simply asking a model to "be more careful" or "double-check" does not
systematically reduce hallucination. The model's errors are often internally
consistent — the same biases that produced the error will reproduce it if the
model is merely asked to re-read its own output. A structured, evidence-based
method of self-verification is needed, one that deliberately breaks the chain of
bias between the initial draft and the verification step.

## Question

How can an LLM systematically verify and correct its own outputs to reduce
hallucination, without requiring external tools or human intervention at every
step?

---

## Key-Line 1: The Four-Step Process

### Step 1 — Generate Baseline Response (Draft)

The model generates an initial response to the user's query using its standard
capabilities. This draft may contain hallucinated facts, incomplete lists, or
unsupported claims.

> "Given a query, the LLM generates a baseline response. This may contain
> inaccuracies (hallucinations) that we aim to address in subsequent steps."
>
> — Dhuliawala et al. (2023), Section 3

### Step 2 — Plan Verification Questions

The model analyses its own draft and generates a set of **verification
questions** designed to fact-check the specific claims made in the draft. These
questions must be:

- **Targeted**: Each question addresses a specific factual claim in the draft.
- **Independently answerable**: The question can be answered without reference to
  the draft.
- **Empirically verifiable**: The question has a concrete, checkable answer.

For example, if the draft claims "Marie Curie was born in Warsaw, Poland", the
verification question might be: "Where was Marie Curie born?"

### Step 3 — Execute Verification (Independent Answers)

The model answers each verification question **independently** — that is,
without conditioning on the original draft. This is the critical innovation.

> "The key idea is that the verification answers should be generated
> independently of the original response so that they are not biased by it."
>
> — Dhuliawala et al. (2023), Section 3

The paper explores several execution strategies:

| Strategy | Description | Bias Control |
|---|---|---|
| **Joint** | Answer all questions in a single pass alongside the draft. | Low — still conditioned on the draft. |
| **2-Step** | First generate draft + questions, then answer questions in a separate pass. | Medium — questions are separate but may share context. |
| **Factored** | Answer each question in a completely independent prompt, one at a time. | High — each answer is derived from scratch. |

The **Factored** variant produces the best results because it maximally isolates
each verification from the original draft's biases.

### Step 4 — Generate Final Verified Response

The model produces a final response that incorporates the verified answers. Where
a verification answer contradicts the original draft, the draft is corrected.
Where the draft is confirmed, it is retained. The result is a response with
measurably fewer hallucinated claims.

```text
┌─────────────────────────────────────────────────┐
│  User Query                                     │
└──────────────────┬──────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────┐
│  Step 1: Generate Baseline Draft                │
└──────────────────┬──────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────┐
│  Step 2: Plan Verification Questions            │
│  (targeted, independently answerable)           │
└──────────────────┬──────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────┐
│  Step 3: Answer Questions Independently         │
│  (factored — no reference to draft)             │
└──────────────────┬──────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────┐
│  Step 4: Generate Final Verified Response        │
│  (correct draft using verified answers)          │
└─────────────────────────────────────────────────┘
```

---

## Key-Line 2: Independence Is the Critical Factor

The most important design decision in CoVe is that verification answers must be
produced independently of the draft. When the model verifies claims while still
"looking at" the draft, the same biases that caused the hallucination persist.
Factored verification — where each question is answered in its own prompt — is
the most effective variant precisely because it eliminates this conditioning.

## Key-Line 3: CoVe Is Domain-Agnostic

The paper demonstrates CoVe across three distinct task types:

- **List-based questions** (Wikidata): "Name some politicians born in
  Massachusetts." CoVe reduced hallucinated list items significantly.
- **Closed-book QA** (MultiSpanQA): Short-answer factual questions where CoVe
  improved accuracy by correcting individual factual claims.
- **Longform text generation**: Open-ended text where CoVe identified and
  corrected unsupported statements within paragraphs of generated content.

This breadth shows the method is not specific to a single kind of output.

## Key-Line 4: CoVe Requires No External Tools

CoVe is a purely prompt-based technique. It does not require:

- Access to a search engine or retrieval-augmented generation (RAG).
- External fact-checking databases.
- Human-in-the-loop review.
- Fine-tuning or model retraining.

The model uses its own knowledge, accessed through a different prompting pathway,
to verify itself. This makes CoVe deployable immediately with any sufficiently
capable LLM.

## Key-Line 5: Practical Application in Software Engineering

CoVe has been adapted for software engineering agent workflows. In this
repository, the CoV pattern is used systematically:

1. **Initial draft** — The agent restates requirements and proposes a plan.
2. **Claim list** — Atomic, testable statements are extracted.
3. **Verification questions** — Five to ten questions that would expose errors.
4. **Independent answers** — Evidence-based answers derived from repository code,
   tests, and configuration, not from the draft.
5. **Revised plan** — The plan is updated based on verified answers.
6. **Implementation** — Only after the revised plan is confirmed.

This is the process described in the CoV Mississippi agents used throughout this
repository.

---

## Evidence and Results

The experimental results from the paper (Dhuliawala et al., 2023) show:

- On Wikidata list questions, CoVe with factored verification reduced the
  hallucination rate substantially compared to both the baseline and the
  joint-verification variant.
- On MultiSpanQA, the factored approach improved precision without sacrificing
  recall.
- The improvement is consistent across model sizes, though larger models benefit
  more because they have richer internal knowledge to draw upon during
  verification.

---

## Authoritative Sources

| Source | Reference |
|---|---|
| **Research paper** | Dhuliawala, S., Komeili, M., Xu, J., Raileanu, R., Li, X., Celikyilmaz, A., & Weston, J. (2023). *Chain-of-Verification Reduces Hallucination in Large Language Models*. arXiv:2309.11495. <https://arxiv.org/abs/2309.11495> |
| **Authors** | Meta AI Research (FAIR). The paper was submitted 20 September 2023 and revised 25 September 2023. |
| **Citation** | Cite as: arXiv:2309.11495 [cs.CL]. DOI: <https://doi.org/10.48550/arXiv.2309.11495> |
| **Related work** | Chain-of-Thought prompting (Wei et al., 2022); Self-Consistency (Wang et al., 2022); Retrieval-Augmented Generation (Lewis et al., 2020). CoVe differs from these by focusing on post-hoc self-verification rather than retrieval or sampling diversity. |

---

## Summary

CoVe is a four-step self-verification method for LLMs: draft, plan verification
questions, answer independently, revise. The critical insight is that
verification must be independent of the draft to avoid reproducing the same
biases. The method is domain-agnostic, requires no external tools, and has been
empirically shown to reduce hallucination across list, QA, and longform tasks.
In software engineering agent workflows, it translates into a disciplined
plan-verify-implement cycle that catches errors before they reach code.
