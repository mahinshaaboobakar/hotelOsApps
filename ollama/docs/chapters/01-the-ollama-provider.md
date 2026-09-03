# 01 · The ollama provider

**HotelOS's first shippable `kind: provider` package.** It gives a property
local models — running on the hotel's own hardware, through an Ollama server the
hotel runs — and it is the package the whole provider mechanism was designed
against.

Governed by platform **ADR 0130** (the AI execution substrate), **PKG-Q44** (the
manifest's tagged union), **PKG-Q45** (a provider installs; and the declared API
dialect) and **AI-Q3** (a URL is configuration, a key is not).

---

## 1 · What it is

A provider package is **declarative-only**. This one ships:

```text
ollama/
  manifest.yaml                 the whole package
  docs/chapters/                this
```

and nothing else. There is no backend, no `ui/`, no schema, no migration, no
process and no principal. The signed archive carries `files: {}` — an empty
inventory, present rather than omitted, because *"an absent inventory and an
empty one are different claims, and only the second is true"*.

The platform enforces that rather than trusting it: a provider carrying a file
is refused as `ProviderShipsFiles`, and a provider manifest has nowhere to write
a `runtime:` block at all — the union's provider arm has no such field, so it is
*inexpressible* rather than validated-and-rejected (ADR 0130 §3).

**What installing it does** is add a row the Kernel can serve from. The Model
Gateway asks `ListProviders`, receives this declaration, and builds a routing
table out of it. Nothing starts.

---

## 2 · What it declares

### 2.1 The endpoint is the property's, not the package's

```yaml
endpoint:
  configuration: ai.providers.ollama.base_url
```

A public provider's endpoint is a fact about the provider and ships signed.
Ollama's is a fact about *this hotel* — which machine on the property runs the
models — so the manifest names the **configuration key** the URL is read from,
and the property fills it in.

The key is written here rather than assembled by convention. A Gateway that
derived `ai.providers.<id>.base_url` itself would be a second place the key is
spelled, and this package could then never move it.

The same key is declared in the manifest's `configuration:` block, with the
default `http://127.0.0.1:11434` and `scope: property`. Both entries are
required: the Kernel's configuration store refuses a write to a key nobody
declared, so an endpoint naming an undeclared key would install and then be
unsettable. Property scope, not user — which machine runs the models is the
hotel's answer, and a per-user override would let one person point the
platform's AI at something nobody else could see.

### 2.2 No credential exists to hold

```yaml
authentication:
  mode: none
```

A local server needs no key, and `mode` is also what renders the configuration
screen: this is why the platform asks for a URL and asks for no key. A provider
declaring authentication without a vault path is refused
(`AuthenticationWithoutCredential`), and a path outside the `providers/` subtree
is refused too — the package never ships a key in either case.

### 2.3 The API dialect — `PKG-Q45(b)`

```yaml
api_dialect: openai_chat_completions
```

**Ollama's OpenAI-compatible surface, not its native one.** It serves both:
`/api/chat` in its own shape, and `/v1/chat/completions` in OpenAI's. This
package declares the compatible one, so the platform needs no Ollama-specific
transport code — which is what keeps *a hotel adds a model* from becoming *a
hotel needs a platform release*.

The field exists because it was found missing the hard way. The Gateway's
transport spoke Anthropic's `POST /v1/messages` to every provider; this package
would have `404`ed **with the server running and the model pulled**. A closed
enum of dialects the transport actually implements is the fix, and an unknown
one is refused before anything is sent.

### 2.4 The catalogue is a claim about the package, not the property

Three models are declared: two for conversation and one for embeddings. **A
property that has pulled none of them still installs this package**, and that
gap is real rather than papered over — the declaration says what Ollama
*offers*, not what this machine currently *holds*.

What the platform does about it is name the difference when a call fails. A
model server that is not running reads as `unreachable`; one that is running
without the model pulled reads as `refused (client, 404)`. Those are different
things an operator does — *start the service* and *run `ollama pull`* — and
before `AI-Q13`'s revisit they produced the same sentence.

---

## 3 · Installing and configuring it

1. Install the signed `.hopkg` through Software Center. No permissions are
   requested, so there is nothing to approve — a provider asks for none.
2. Set `ai.providers.ollama.base_url` if the models run somewhere other than
   this machine. The default is `http://127.0.0.1:11434`.
3. Pull the models on the server: `ollama pull llama3.1:8b`, and the others as
   the property wants them.

Uninstalling removes the row; the Gateway's next read simply does not see it.
An uninstalled provider offers nothing, and a Gateway that could still see one
could still route to it.

---

## 4 · What this package deliberately does not do

| | |
|---|---|
| **Install or manage Ollama** | it is a declaration about a server the property runs, not a bundle of one |
| **Pull models** | a property's disk and its choices; the platform never downloads model weights on someone's behalf |
| **Name itself to applications** | applications say what they *need*; the Gateway decides what satisfies it. No application ever names `ollama` |
| **Hold a credential** | there is none, and there is nowhere in a provider manifest to put one |

---

## 5 · Building the package

```bash
node ../scripts/build-provider.mjs ollama
```

The build stages the manifest alone and then signs it. A package with no payload
still needs a build step, for a reason worth knowing: `hopkg` inventories every
file it finds under the directory it is given, so signing this directory
directly would sign `docs/` as payload — and the platform would then refuse the
result as a provider shipping files. The script exists to make that impossible
rather than to make it survivable.

The signing key never enters this repository. It is supplied at invocation.
