variable "region" {
  description = "AWS region to deploy into (pick the one closest to your members)."
  type        = string
  default     = "us-east-1"
}

variable "instance_type" {
  description = "EC2 instance type. t3.micro is free-tier eligible for 12 months."
  type        = string
  default     = "t3.micro"
}

variable "key_name" {
  description = "Name of an EXISTING EC2 key pair in this region (for SSH). Create/import it first."
  type        = string
}

variable "ssh_allowed_cidr" {
  description = "CIDR allowed to SSH (port 22). Use your own IP, e.g. 203.0.113.4/32 — never 0.0.0.0/0."
  type        = string
}

variable "domain" {
  description = "Subdomain the wallet site serves on, e.g. verify.forgottennft.com."
  type        = string
}

variable "acme_email" {
  description = "Email for Let's Encrypt certificate notices."
  type        = string
}
