# 01 · The OpenAI provider

**Hosted frontier models, as an installable HotelOS provider** — and the other
half of the endpoint design. `ollama` proved the configured arm; this proves the
arm it exists beside.

Governed by platform **ADR 0130** (the AI execution substrate), **PKG-Q44**
(the manifest's tagged union), **PKG-Q45** (a provider installs; the declared
API dialect), **AI-Q3** (a URL is configuration, a key is not) and **AI-Q13**
(refusal fidelity).

---

## 1 · The two endpoint arms, side by side

```text
ollama    endpoint.configuration    the hotel says where its server is
openai    endpoint.url              there is one address and we know it
```

A public provider's endpoint is a fact **about the provider** — identical on
every property — so it ships signed in the package and there is no configuration
key to leave unset. A local server's is a fact about the hotel.

Two arms rather than one string with a magic value, so neither case can be
written as the other. This package is why the `oneof` exists rather than a
convention.

The URL is the **origin only**: `https://api.openai.com`. The transport appends
the path its dialect defines, which is what stops this field carrying a second,
silent claim about which API is spoken.

---

## 2 · The credential — a path, not a key

```yaml
authentication:
  mode: bearer_token
  secret: providers/openai/api_key
```

**The package ships no key and could not.** `secret` is a vault path — a name.
ADR 0130 §2: the value is entered at configuration, held in the vault, and read
by the Model Gateway alone, fetched at call time so a rotated key takes effect
on the next call and never sits on the transport.

The path is bounded to `providers/` three times over, and each check answers a
different person:

| Check | Refuses | Tells |
|---|---|---|
| the validator, at signing and install | a manifest naming a path outside `providers/` | the **publisher**, before it ships |
| the Kernel's policy gate, at read | the AI Runtime reading outside `providers/` | nobody — it is a wall |
| `set-provider-secret`, at sealing | a key sealed outside `providers/` | the **operator**, at the console |

The third was added with this package, and its absence is worth recording: a key
sealed at `provider/openai/api_key` would have been stored successfully and
never found, and the symptom would have been an authentication error from
OpenAI, three layers from the typo.

### 2.1 Sealing the key

```bash
$env:KEY | hotelos-kernel set-provider-secret providers/openai/api_key
```

The **path** is the argument and the **key** is read from standard input — never
argv, so it is not in `--help`, not in shell history and not in a process
listing (ADR 0090 §Q29). The command prints the path back and never the value.

---

## 3 · The catalogue, and why the costs matter more than they look

The degradation ladder **orders by cost**, so these numbers decide what a
property's money is spent on. Four models are declared:

| Model | Latency | Published per 1M in / out | For |
|---|---|---|---|
| `gpt-4o` | standard | $2.50 / $10.00 | the capable rung |
| `gpt-4o-mini` | fast | $0.15 / $0.60 | the cheap rung the ladder steps down to |
| `text-embedding-3-small` | fast | $0.02 / — | retrieval |
| `text-embedding-3-large` | fast | $0.13 / — | retrieval, when quality beats storage |

`gpt-4o-mini` is sixteen times cheaper in, and that is what makes degradation
mean something: a cheap rung that is still capable is the difference between
degrading and refusing.

**The catalogue is deliberately short.** Only models whose pricing is known with
confidence are declared. A model omitted is one the Gateway will not route to,
which is recoverable; a model declared at a guessed price sorts wrongly against
every other rung and nobody finds out from a failure. **Verify the figures
against OpenAI's current pricing before a property routes real spend** — a stale
price here does not fail, it quietly misroutes.

Embeddings carry a true zero output cost, because embeddings have no output
tokens — not an unknown written as zero.

---

## 4 · What a turn looks like

```text
person's message
   → Kernel admission (ai_agent.execute)
   → Policy → input Guardrails
   → agent engine
   → Model Gateway          resolve: openai/gpt-4o, then openai/gpt-4o-mini
   → HttpProviderTransport  POST https://api.openai.com/v1/chat/completions
                            Authorization: Bearer «from providers/openai/api_key»
   → choices[0].message.content, usage.prompt_tokens / completion_tokens
   → output Guardrails → Approval-for-writes → Audit
```

The dialect decides the last two lines: the path posted to, and where the answer
and the token counts are read from. Declaring
`api_dialect: openai_chat_completions` is what lets one transport serve this
provider and a local Ollama imitating it, with no code for either.

---

## 5 · When it fails, and what an operator does

`AI-Q13`'s taxonomy, against a provider that actually answers:

| Reads as | Means | Do |
|---|---|---|
| `refused (client, 401)` | the key is missing, wrong, or revoked | seal it again |
| `refused (client, 429)` | rate or quota limit | wait, or raise the account's limit |
| `refused (server, 5xx)` | OpenAI's own health | wait; this is not the property's |
| `unreachable` | no egress from this property | the network, not the account |
| `timed out` | connected, no answer in time | do not restart anything |

The `client` / `server` split is the useful half: a 4xx sends an operator to
this property's configuration, a 5xx to the vendor's status page. Observed on
this package's first live run — an invalid key produced exactly
`refused (client, 401)`, naming both models the ladder tried.

---

## 6 · What this package deliberately does not do

| | |
|---|---|
| **Hold the key** | it names a path; the value is sealed on the property |
| **Name itself to applications** | applications say what they *need*; the Gateway decides what satisfies it |
| **Choose a model for a caller** | the ladder does, by capability, context, latency and cost |
| **Declare every OpenAI model** | only those whose price is known; see §3 |

---

## 7 · Building the package

```bash
node ../scripts/build-provider.mjs openai --key <path outside this repository>
```

Signed with `files: {}` — no backend, no `ui/`, no schema, no process. The
signing key never enters this repository.
