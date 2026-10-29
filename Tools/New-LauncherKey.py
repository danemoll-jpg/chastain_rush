"""Round 81 (J): Chastain Rush's own publisher key, made once (Woodstock Rush's New-LauncherKey.py; never Woodstock Rush's key). Create/reuse the
local key in Builds/PublisherPrivate/ (git-ignored, never printed, never deleted); only the public modulus enters source (Launcher/pinned_key.hpp)."""
from pathlib import Path
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives import serialization
import hashlib,json
root=Path(__file__).resolve().parents[1]
private=root/'Builds/PublisherPrivate/launcher-signing.pem'
private.parent.mkdir(parents=True,exist_ok=True)
if private.exists():
    key=serialization.load_pem_private_key(private.read_bytes(),password=None)
else:
    key=rsa.generate_private_key(public_exponent=65537,key_size=3072)
    private.write_bytes(key.private_bytes(serialization.Encoding.PEM,serialization.PrivateFormat.PKCS8,serialization.NoEncryption()))
public=key.public_key();numbers=public.public_numbers()
der=public.public_bytes(serialization.Encoding.DER,serialization.PublicFormat.SubjectPublicKeyInfo)
record={'algorithm':'RSA-PKCS1-SHA256','keyId':hashlib.sha256(der).hexdigest(),
        'modulusHex':format(numbers.n,'x'),'exponentHex':format(numbers.e,'06x')}
(root/'Launcher/pinned-key.json').write_text(json.dumps(record,indent=2))
(root/'Launcher/pinned_key.hpp').write_text('#pragma once\nnamespace wr {\ninline constexpr const char* KeyId="'+record['keyId']+'";\ninline constexpr const char* ModulusHex="'+record['modulusHex']+'";\ninline constexpr const char* ExponentHex="'+record['exponentHex']+'";\n}\n')
print('Public key ID:',record['keyId'])
print('Private publisher key kept only in excluded Build storage:',private)
