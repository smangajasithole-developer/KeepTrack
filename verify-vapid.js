const crypto = require("node:crypto");
const { execFileSync } = require("node:child_process");

const output = execFileSync("dotnet", ["user-secrets", "list"], {
    encoding: "utf8",
    stdio: ["ignore", "pipe", "ignore"]
});

const secrets = {};

for (const line of output.split(/\r?\n/)) {
    const match = line.match(/^(Vapid:PublicKey|Vapid:PrivateKey)\s*=\s*(.*)$/);
    if (match) secrets[match[1]] = match[2].trim();
}

if (!secrets["Vapid:PublicKey"] || !secrets["Vapid:PrivateKey"]) {
    console.log("Could not read both VAPID keys.");
    process.exit(1);
}

try {
    const privateKey = Buffer.from(secrets["Vapid:PrivateKey"], "base64url");
    const suppliedPublicKey = Buffer.from(secrets["Vapid:PublicKey"], "base64url");

    const ecdh = crypto.createECDH("prime256v1");
    ecdh.setPrivateKey(privateKey);

    const derivedPublicKey = ecdh.getPublicKey();

    console.log(
        "Public key matches private key:",
        derivedPublicKey.equals(suppliedPublicKey) ? "YES" : "NO"
    );
} catch {
    console.log("Could not validate the key pair or key format.");
    process.exit(1);
}
