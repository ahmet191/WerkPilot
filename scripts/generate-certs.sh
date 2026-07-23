#!/usr/bin/env bash
#
# Generates self-signed signing + encryption certificates for OpenIddict.
#
# The API refuses to start outside Development without real signing/encryption
# certificates (see Program.cs). This produces a usable pair for a demo or a
# self-hosted deployment. A real production system should instead use CA-issued
# certificates or a KMS/HSM-backed key — treat these .pfx files as secrets and
# never commit them (the repo .gitignore already excludes *.pfx via certs/).
#
# Usage:
#   scripts/generate-certs.sh [output_dir]        # default: ./certs
#   CERT_PASSWORD=... scripts/generate-certs.sh   # default password: changeit
#
set -euo pipefail

OUT_DIR="${1:-./certs}"
PASSWORD="${CERT_PASSWORD:-changeit}"
mkdir -p "$OUT_DIR"

gen() {
  local name="$1"
  openssl req -x509 -newkey rsa:2048 -nodes \
    -keyout "$OUT_DIR/$name.key" \
    -out "$OUT_DIR/$name.crt" \
    -days 3650 -subj "/CN=WerkPilot $name" >/dev/null 2>&1
  openssl pkcs12 -export \
    -inkey "$OUT_DIR/$name.key" \
    -in "$OUT_DIR/$name.crt" \
    -out "$OUT_DIR/$name.pfx" \
    -passout "pass:$PASSWORD"
  rm -f "$OUT_DIR/$name.key" "$OUT_DIR/$name.crt"
}

gen signing
gen encryption

cat <<EOF

Wrote $OUT_DIR/signing.pfx and $OUT_DIR/encryption.pfx (password: $PASSWORD)

Point the API at them (env vars, or mount the dir and set paths in config):
  OpenIddict__SigningCertificate__Path=/certs/signing.pfx
  OpenIddict__SigningCertificate__Password=$PASSWORD
  OpenIddict__EncryptionCertificate__Path=/certs/encryption.pfx
  OpenIddict__EncryptionCertificate__Password=$PASSWORD

Under docker, mount the dir and pass those env vars, e.g.:
  volumes:  ["./certs:/certs:ro"]
EOF
