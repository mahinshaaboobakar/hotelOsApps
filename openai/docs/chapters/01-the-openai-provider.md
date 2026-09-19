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
property's money is spent on. `manifest.yaml` is the list; this table mirrors it:

| Model | Latency | Published per 1M in / out | Context · max out | For |
|---|---|---|---|---|
| `gpt-4o` | standard | $2.50 / $10.00 | 128,000 · 16,384 | the capable rung |
| `gpt-4o-mini` | fast | $0.15 / $0.60 | 128,000 · 16,384 | the cheap rung the ladder steps down to |
| `gpt-5-mini` | fast | $0.25 / $2.00 | 400,000 · 128,000 | GPT-5, low latency |
| `gpt-5-nano` | fast | $0.05 / $0.40 | 400,000 · 128,000 | the cheapest chat rung declared |
| `gpt-5.4-mini` | standard | $0.75 / $4.50 | 400,000 · 128,000 | GPT-5.4, coding and agents |
| `gpt-5.4-nano` | standard | $0.20 / $1.25 | 400,000 · 128,000 | GPT-5.4, simple high-volume |
| `text-embedding-3-small` | fast | $0.02 / — | 8,191 | retrieval |
| `text-embedding-3-large` | fast | $0.13 / — | 8,191 | retrieval, when quality beats storage |
| `whisper-1` | fast | $0.006 per minute | — | speech to text |
| `tts-1` | fast | $15.00 per 1M characters | — | text to speech |

*This table said "Four models are declared" until 1.1.0, while the manifest
declared six — a count in prose goes stale the day the list grows, so it no
longer carries one.*

### 3.0 Where the GPT-5-family figures come from — read 2026-09-19

Only OpenAI's own documentation, and every figure from two pages that agreed:

```text
pricing   https://developers.openai.com/api/docs/pricing       Standard tier, "Short context"
models    https://developers.openai.com/api/docs/models/<id>   context, max output, features
```

- **Prices are the Standard tier.** The page also lists Batch and Flex at half
  price, names no default tier, and publishes no long-context price for these
  four — so the tier is stated, not assumed.
- **Capabilities are only what each model page lists as supported:**
  `function_calling` → `tools`, `structured_outputs` → `json_schema`,
  `streaming` → `streaming`, `image_input` → `vision`. Every one of the four
  lists Chat Completions as *Supported*, which is the dialect this package speaks.
- **`long_context` is not declared on any of them**, although each takes
  400,000 tokens. It is the platform's term — *"a context window the platform
  treats as large"* — with no ruled threshold, so it is not something OpenAI's
  documentation can state. (The older lines already disagree about it:
  `gpt-4o` declares it at 128,000 and `gpt-4o-mini` does not, at the same
  128,000.)
- **Latency** comes from each page's own description: `fast` where OpenAI claims
  speed (*"low latency"*, *"Fastest"*), `standard` otherwise. OpenAI publishes no
  latency class.
- **Cached-input prices** exist for all four and have no field in this schema.

**Not established from OpenAI's documentation, and it matters.** The Gateway's
`openai_chat_completions` dialect sends `max_tokens`. Whether the GPT-5 family
accepts `max_tokens` on Chat Completions, or requires `max_completion_tokens`,
could not be read: the Chat Completions API reference answered 403 at
`platform.openai.com` and 404 at `developers.openai.com`, and the reasoning guide
does not say. If they refuse it, every call to them comes back
`refused (client, 400)` and the ladder steps past them — recoverable, and
visible in §5's taxonomy, rather than a misroute.

`gpt-4o-mini` is sixteen times cheaper in than `gpt-4o`, and that is what makes degradation
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

### 3.1 The catalogue is the provider's truth, not the property's policy

**This list is what OpenAI offers.** A property that wants to spend less does
not edit it — editing it would make the package lie about the vendor, and put a
hotel's policy inside a file the vendor signs.

The property's bound is a platform configuration key:

```text
ai.routing.pinned_model = openai/gpt-4o-mini
```

Unset, the ladder routes by the resolver's design. Set, **every call the pinned
model can serve goes through it** and nothing ladders past it — a bound, not a
weight. It applies per capability it satisfies, so a pinned conversation model
does not bind embedding requests, and a pin naming a model nobody installed
refuses namefully rather than quietly spending elsewhere.

`AI-Q15`'s Settings screen will edit the same key. Until then it is set through
the configuration store.

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
