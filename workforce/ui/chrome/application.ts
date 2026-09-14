/**
 * What this application is called, where a person reads it.
 *
 * One constant with three readers — the title bar, a widget's header, and the
 * sentence a failure draws. The third is new: `@hotelos/sdk` writes the failure
 * words for every application and therefore carries none of their names, so it
 * asks for this one by parameter. Writing `"Workforce"` a fourth time would
 * have made a rename a search rather than an edit.
 */

/**
 * The name, as it is shown.
 *
 * Not the package id — that is `workforce`, it appears on the wire and in the
 * manifest, and a sentence reading *workforce did not answer* names the right
 * thing at the wrong register.
 */
export const APPLICATION = "Workforce";
