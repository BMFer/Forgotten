output "public_ip" {
  description = "Elastic IP — create an A record for your subdomain pointing here."
  value       = aws_eip.forgotten.public_ip
}

output "instance_id" {
  value = aws_instance.forgotten.id
}

output "next_steps" {
  value = <<-EOT
    1) At GoDaddy, add a DNS A record: ${var.domain} -> ${aws_eip.forgotten.public_ip}
    2) Wait for DNS to propagate, then SSH in: ssh -i <your-key.pem> ubuntu@${aws_eip.forgotten.public_ip}
    3) Create /etc/forgotten/forgotten.env from deploy/forgotten.env.example (chmod 600).
    4) From the repo, run: deploy/publish-and-deploy.sh ${aws_eip.forgotten.public_ip} <your-key.pem>
    5) Browse https://${var.domain}/health
  EOT
}
