# 1. 生成私鑰   輸入密碼:1qaz@WSX
openssl genpkey -algorithm RSA -out private.key -aes256

# 2. 生成 CSR，使用 SHA-256   輸入密碼:1qaz@WSX
openssl req -new -key private.key -out request.csr -sha256

# 輸入
# Country Name (2 letter code) [AU]:TW
# State or Province Name (full name) [Some-State]:Taiwan
# Locality Name (eg, city) []:Taipei
# Organization Name (eg, company) [Internet Widgits Pty Ltd]:JEPUN
# Organizational Unit Name (eg, section) []:RD
# Common Name (e.g. server FQDN or YOUR name) []:For PDF SIGN
# Email Address []:youlinchen@jepun.com.tw

# Please enter the following 'extra' attributes
# to be sent with your certificate request
# A challenge password []:1qaz@WSX
# An optional company name []:JP




# 3. 自簽署數位證書，使用 SHA-256    輸入密碼:1qaz@WSX
openssl x509 -req -days 365 -in request.csr -signkey private.key -out certificate.crt -sha256

# 4. 生成 PFX 憑證   輸入密碼:1qaz@WSX
openssl pkcs12 -export -out certificate.pfx -inkey private.key -in certificate.crt
