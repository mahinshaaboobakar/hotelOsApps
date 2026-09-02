/**
 * The entry the build compiles into `ui/module.js`.
 *
 * Fire and forget: the promise resolves when the host has handshaken and the
 * form is mounted, and there is nobody here to hand it to. A rejection is the
 * host's to report — a package cannot draw its own failure to connect on a
 * surface it was never given.
 */
import { connectToHost } from "@hotelos/sdk";

import { activate } from "./application";

void connectToHost(activate);
